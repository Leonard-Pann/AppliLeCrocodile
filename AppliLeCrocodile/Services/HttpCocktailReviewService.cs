using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using AppliLeCrocodile.Infrastructure.Assets;
using AppliLeCrocodile.Infrastructure.Serialization;
using AppliLeCrocodile.Models;
using LeCrocodile.Reviews.Contracts;

namespace AppliLeCrocodile.Services;

public sealed class HttpCocktailReviewService : ICocktailReviewService
{
    private readonly HttpClient client;
    private readonly IInstallationIdentity identity;
    private readonly IAssetReader assetReader;
    private readonly SemaphoreSlim requestGate = new SemaphoreSlim(1, 1);
    private Uri? baseUri;
    private DateTimeOffset nextRequestAt;

    public HttpCocktailReviewService(HttpClient client, IInstallationIdentity identity, IAssetReader assetReader)
    {
        this.client = client;
        this.identity = identity;
        this.assetReader = assetReader;
    }

    public bool IsDemo => false;

    public async Task<IReadOnlyDictionary<string, ReviewSummary>> GetSummariesAsync(
        IReadOnlyList<string> cocktailIds, CancellationToken cancellationToken)
    {
        ReviewSummary[] summaries = await SendAsync(HttpMethod.Get, "api/v1/cocktails/review-summaries", null,
            ReviewApiJsonContext.Default.ReviewSummaryArray, cancellationToken).ConfigureAwait(false);
        HashSet<string> requestedIds = cocktailIds.ToHashSet(StringComparer.Ordinal);
        if (summaries.Any(summary => summary.TotalCount <= 0 || summary.AverageRating is < 1 or > 5
            || !double.IsFinite(summary.AverageRating))
            || summaries.Select(summary => summary.CocktailId).Distinct(StringComparer.Ordinal).Count() != summaries.Length)
        {
            throw new InvalidDataException("The review API returned invalid rating summaries.");
        }

        return summaries.Where(summary => requestedIds.Contains(summary.CocktailId))
            .ToDictionary(summary => summary.CocktailId, StringComparer.Ordinal);
    }

    public async Task<ReviewPage> GetReviewPageAsync(string cocktailId, long? before, CancellationToken cancellationToken)
    {
        string path = $"api/v1/cocktails/{Uri.EscapeDataString(cocktailId)}/reviews";
        if (before is not null)
        {
            path += $"?before={before.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        }

        ReviewPage page = await SendAsync(HttpMethod.Get, path, null,
            ReviewApiJsonContext.Default.ReviewPage, cancellationToken).ConfigureAwait(false);
        if (page.Items is null || page.Items.Length > 100 || page.TotalCount < page.Items.Length
            || page.AverageRating is < 0 or > 5 || !double.IsFinite(page.AverageRating)
            || (page.TotalCount == 0) != (page.AverageRating == 0)
            || page.Items.Any(review => review.Id <= 0 || string.IsNullOrWhiteSpace(review.UserName)
                || review.UserName.Length > 40 || review.Rating is < 1 or > 5 || review.Comment is null
                || review.Comment.Length > 2000)
            || page.Items.Select(review => review.Id).Distinct().Count() != page.Items.Length
            || !page.Items.Select(review => review.Id).SequenceEqual(page.Items.Select(review => review.Id).OrderDescending())
            || (before is not null && page.Items.Any(review => review.Id >= before.Value))
            || (page.NextBefore is not null && (page.Items.Length == 0 || page.NextBefore != page.Items[^1].Id))
            || page.NextBefore is <= 0 || (before is not null && page.NextBefore >= before))
        {
            throw new InvalidDataException("The review API returned an invalid review page.");
        }

        return page;
    }

    public async Task SubmitReviewAsync(string cocktailId, CocktailReview review, CancellationToken cancellationToken)
    {
        SubmitReviewRequest request = new SubmitReviewRequest(review.UserName, review.Rating, review.Comment);
        byte[] body = await Task.Run(() => JsonSerializer.SerializeToUtf8Bytes(
            request, ReviewApiJsonContext.Default.SubmitReviewRequest), cancellationToken).ConfigureAwait(false);
        await SendAsync(HttpMethod.Put, $"api/v1/cocktails/{Uri.EscapeDataString(cocktailId)}/reviews", body,
            ReviewApiJsonContext.Default.ReviewResponse, cancellationToken).ConfigureAwait(false);
    }

    private Task<T> SendAsync<T>(
        HttpMethod method, string path, byte[]? body, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            return await SendCoreAsync(method, path, body, typeInfo, timeout.Token).ConfigureAwait(false);
        }, cancellationToken);
    }

    private async Task<T> SendCoreAsync<T>(
        HttpMethod method, string path, byte[]? body, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken)
    {
        Guid userId = await identity.GetOrCreateAsync(cancellationToken).ConfigureAwait(false);
        await requestGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (baseUri is null)
            {
                string json = await assetReader.ReadTextAsync("Assets/Configuration/reviews-api.json", cancellationToken)
                    .ConfigureAwait(false);
                ReviewApiSettings? settings = await Task.Run(() => JsonSerializer.Deserialize(
                    json, ReviewApiJsonContext.Default.ReviewApiSettings), cancellationToken).ConfigureAwait(false);
                if (!Uri.TryCreate(settings?.BaseUrl, UriKind.Absolute, out Uri? configuredUri)
                    || configuredUri.Scheme != Uri.UriSchemeHttps || configuredUri.Query.Length > 0
                    || configuredUri.Fragment.Length > 0 || configuredUri.UserInfo.Length > 0)
                {
                    throw new InvalidDataException(
                        "The review API base URL must be an absolute HTTPS URL without credentials or query.");
                }

                baseUri = new Uri(configuredUri.AbsoluteUri.TrimEnd('/') + "/");
            }

            string separator = path.Contains('?') ? "&" : "?";
            Uri uri = new Uri(baseUri, $"{path}{separator}userId={userId:D}");
            for (int attempt = 0; attempt < 2; attempt++)
            {
                TimeSpan delay = nextRequestAt - DateTimeOffset.UtcNow;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }

                using HttpRequestMessage request = new HttpRequestMessage(method, uri);
                request.Headers.Accept.ParseAdd("application/json");
                if (body is not null)
                {
                    request.Content = new ByteArrayContent(body);
                    request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                }

                nextRequestAt = DateTimeOffset.UtcNow.AddMilliseconds(220);
                using HttpResponseMessage response = await client.SendAsync(
                    request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                if (attempt == 0 && response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
                {
                    TimeSpan retryDelay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(1);
                    nextRequestAt = DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(Math.Clamp(retryDelay.TotalSeconds, 1, 5)));
                    continue;
                }

                response.EnsureSuccessStatusCode();
                await response.Content.LoadIntoBufferAsync(1024 * 1024, cancellationToken).ConfigureAwait(false);
                await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                T? result = await JsonSerializer.DeserializeAsync(stream, typeInfo, cancellationToken).ConfigureAwait(false);
                return result ?? throw new InvalidDataException("The review API returned an empty JSON response.");
            }

            throw new HttpRequestException("The review API request could not be completed.");
        }
        finally
        {
            requestGate.Release();
        }
    }
}
