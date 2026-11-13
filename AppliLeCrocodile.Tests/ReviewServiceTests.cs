using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using AppliLeCrocodile.Infrastructure.Assets;
using AppliLeCrocodile.Models;
using AppliLeCrocodile.Services;
using LeCrocodile.Reviews.Contracts;

namespace AppliLeCrocodile.Tests;

internal static class ReviewServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    private static int assertions;

    public static async Task<int> RunAsync()
    {
        await VerifyIdentityAsync();
        await VerifyHttpServiceAsync();
        return assertions;
    }

    private static async Task VerifyIdentityAsync()
    {
        string directory = Path.Combine(Path.GetTempPath(), "LeCrocodileIdentityTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "installation-id.txt");
        try
        {
            Task<Guid>[] concurrent = Enumerable.Range(0, 12).Select(_ =>
                new LocalInstallationIdentity(path).GetOrCreateAsync(default)).ToArray();
            Guid[] identities = await Task.WhenAll(concurrent);
            Check(identities[0] != Guid.Empty && identities.Distinct().Count() == 1,
                "Concurrent first launches atomically create one shared installation UUID.");
            Guid reopened = await new LocalInstallationIdentity(path).GetOrCreateAsync(default);
            Check(reopened == identities[0], "The installation UUID survives restarting the application.");
            Guid other = await new LocalInstallationIdentity(Path.Combine(directory, "other-id.txt")).GetOrCreateAsync(default);
            Check(other != reopened, "Separate installations receive different UUIDs.");
            await File.WriteAllTextAsync(path, "invalid");
            bool corruptRejected = false;
            try
            {
                await new LocalInstallationIdentity(path).GetOrCreateAsync(default);
            }
            catch (InvalidDataException)
            {
                corruptRejected = true;
            }

            Check(corruptRejected && await File.ReadAllTextAsync(path) == "invalid",
                "A damaged UUID file is reported without silently changing the user's identity.");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task VerifyHttpServiceAsync()
    {
        Guid installationId = Guid.NewGuid();
        FixedIdentity identity = new FixedIdentity(installationId);
        List<(HttpMethod Method, Uri Uri, string? Body)> requests = [];
        using StubHandler handler = new StubHandler(async request =>
        {
            string? body = request.Content is null ? null : await request.Content.ReadAsStringAsync();
            requests.Add((request.Method, request.RequestUri!, body));
            if (request.RequestUri!.AbsolutePath.EndsWith("review-summaries", StringComparison.Ordinal))
            {
                return JsonResponse(new ReviewSummary[] { new ReviewSummary("ABBAYE", 123, 3.75) });
            }

            if (request.Method == HttpMethod.Put)
            {
                SubmitReviewRequest submitted = JsonSerializer.Deserialize<SubmitReviewRequest>(body!, JsonOptions)!;
                return JsonResponse(new ReviewResponse(1, submitted.UserName, submitted.Rating, submitted.Comment,
                    DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, true));
            }

            return JsonResponse(new ReviewPage(
                [new ReviewResponse(2, "Other", 4, "Live comment", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, false)],
                123, 3.75, 2));
        });
        using HttpClient client = new HttpClient(handler);
        HttpCocktailReviewService service = new HttpCocktailReviewService(
            client, identity, new SettingsReader("https://reviews.example/"));
        IReadOnlyDictionary<string, ReviewSummary> summaries = await service.GetSummariesAsync(["ABBAYE", "ACAPULCO"], default);
        Check(summaries["ABBAYE"].TotalCount == 123 && summaries["ABBAYE"].AverageRating == 3.75,
            "The client uses the API's complete rating aggregate.");
        ReviewPage page = await service.GetReviewPageAsync("BANDIT_D'HONNEUR", 100, default);
        Check(page.TotalCount == 123 && page.Items.Length == 1 && page.NextBefore == 2,
            "The HTTP client preserves pagination and global totals.");
        Check(requests[1].Uri.Query.Contains("before=100", StringComparison.Ordinal)
            && requests[1].Uri.GetComponents(UriComponents.Path, UriFormat.Unescaped).Contains("BANDIT_D'HONNEUR"),
            "Cocktail identifiers containing apostrophes survive URI encoding.");
        await service.SubmitReviewAsync("ABBAYE", new CocktailReview("Visitor", 5, "Bon 🍹"), default);
        Check(requests[^1].Method == HttpMethod.Put
            && JsonSerializer.Deserialize<SubmitReviewRequest>(requests[^1].Body!, JsonOptions)
                is { Rating: 5, Comment: "Bon 🍹" },
            "Submissions use the shared PUT contract and preserve Unicode.");
        Check(requests.All(request => request.Uri.Scheme == "https"
            && request.Uri.Query.Contains($"userId={installationId:D}", StringComparison.Ordinal)),
            "Every API request uses HTTPS and the persistent installation UUID.");

        int attempts = 0;
        using StubHandler retryHandler = new StubHandler(_ =>
        {
            attempts++;
            HttpResponseMessage response = attempts == 1 ? new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                : JsonResponse(Array.Empty<ReviewSummary>());
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(1));
            return Task.FromResult(response);
        });
        using HttpClient retryClient = new HttpClient(retryHandler);
        HttpCocktailReviewService retryService = new HttpCocktailReviewService(
            retryClient, identity, new SettingsReader("https://reviews.example/"));
        await retryService.GetSummariesAsync(["ABBAYE"], default);
        Check(attempts == 2, "The client retries a rate-limit rejection after the advertised delay.");

        using StubHandler failureHandler = new StubHandler(
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        using HttpClient failureClient = new HttpClient(failureHandler);
        HttpCocktailReviewService failingService = new HttpCocktailReviewService(
            failureClient, identity, new SettingsReader("https://reviews.example/"));
        bool submissionFailed = false;
        try
        {
            await failingService.SubmitReviewAsync("ABBAYE", new CocktailReview("Visitor", 3, "Draft"), default);
        }
        catch (HttpRequestException)
        {
            submissionFailed = true;
        }

        Check(submissionFailed, "A failed server response cannot be reported as a successful submission.");
        HttpCocktailReviewService insecureService = new HttpCocktailReviewService(
            failureClient, identity, new SettingsReader("http://reviews.example/"));
        bool insecureRejected = false;
        try
        {
            await insecureService.GetReviewPageAsync("ABBAYE", null, default);
        }
        catch (InvalidDataException)
        {
            insecureRejected = true;
        }

        Check(insecureRejected, "The mobile client rejects a plaintext API URL.");
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        bool canceled = false;
        try
        {
            await service.GetReviewPageAsync("ABBAYE", null, cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }

        Check(canceled, "Canceled page requests do not continue in the background.");
    }

    private static HttpResponseMessage JsonResponse<T>(T value)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(value, JsonOptions), Encoding.UTF8, "application/json")
        };
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }

        assertions++;
    }

    private sealed class FixedIdentity(Guid userId) : IInstallationIdentity
    {
        public Task<Guid> GetOrCreateAsync(CancellationToken cancellationToken) => Task.FromResult(userId);
    }

    private sealed class SettingsReader(string baseUrl) : IAssetReader
    {
        public Task<string> ReadTextAsync(string relativePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(JsonSerializer.Serialize(new { BaseUrl = baseUrl }, JsonOptions));
        }
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handle) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return handle(request);
        }
    }
}
