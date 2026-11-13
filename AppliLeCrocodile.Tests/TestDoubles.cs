using Avalonia.Media.Imaging;
using AppliLeCrocodile.Infrastructure.Assets;
using AppliLeCrocodile.Models;
using AppliLeCrocodile.Services;
using LeCrocodile.Reviews.Contracts;

namespace AppliLeCrocodile.Tests;

internal sealed class FileAssetReader : IAssetReader
{
    public Task<string> ReadTextAsync(string relativePath, CancellationToken cancellationToken)
    {
        return File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, relativePath), cancellationToken);
    }
}

internal sealed class OverrideAssetReader : IAssetReader
{
    private readonly IAssetReader inner = new FileAssetReader();
    private readonly Func<string, string> transformCocktails;

    public OverrideAssetReader(Func<string, string> transformCocktails)
    {
        this.transformCocktails = transformCocktails;
    }

    public async Task<string> ReadTextAsync(string relativePath, CancellationToken cancellationToken)
    {
        string text = await inner.ReadTextAsync(relativePath, cancellationToken);
        return relativePath == "Assets/Save/cocktails.json" ? transformCocktails(text) : text;
    }
}

internal sealed class MemoryFavoriteStore : IFavoriteCocktailStore
{
    private readonly HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);

    public bool FailWrites { get; set; }

    public Task<IReadOnlySet<string>> LoadAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(ids, StringComparer.Ordinal));
    }

    public Task SetFavoriteAsync(string cocktailId, bool isFavorite, CancellationToken cancellationToken)
    {
        if (FailWrites)
        {
            throw new IOException("Simulated storage failure.");
        }

        if (isFavorite)
        {
            ids.Add(cocktailId);
        }
        else
        {
            ids.Remove(cocktailId);
        }

        return Task.CompletedTask;
    }
}

internal sealed class EmptyImageLoader : ICocktailImageLoader
{
    public Task<Bitmap?> LoadAsync(string imagePath, CancellationToken cancellationToken)
    {
        return Task.FromResult<Bitmap?>(null);
    }
}

internal sealed class FixedReviewService : ICocktailReviewService
{
    public bool IsDemo => false;

    public bool FailRequests { get; set; }

    public bool FailSubmissions { get; set; }

    public List<(string CocktailId, CocktailReview Review)> Submissions { get; } = [];

    public TaskCompletionSource? SubmissionGate { get; set; }

    public int PageRequests { get; private set; }

    public Func<long?, ReviewPage>? PageFactory { get; set; }

    public async Task<IReadOnlyDictionary<string, ReviewSummary>> GetSummariesAsync(
        IReadOnlyList<string> cocktailIds, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, IReadOnlyList<CocktailReview>> reviews = await GetReviewsAsync(cocktailIds, cancellationToken);
        return reviews.ToDictionary(entry => entry.Key,
            entry => new ReviewSummary(entry.Key, entry.Value.Count, entry.Value.Average(review => review.Rating)));
    }

    public async Task<ReviewPage> GetReviewPageAsync(string cocktailId, long? before, CancellationToken cancellationToken)
    {
        PageRequests++;
        if (FailRequests)
        {
            throw new IOException("Simulated page request failure.");
        }

        if (PageFactory is not null)
        {
            return PageFactory(before);
        }

        IReadOnlyDictionary<string, IReadOnlyList<CocktailReview>> reviews = await GetReviewsAsync([cocktailId], cancellationToken);
        ReviewResponse[] items = reviews[cocktailId].Select((review, index) => new ReviewResponse(
            index + 1, review.UserName, review.Rating, review.Comment, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, false))
            .ToArray();
        return new ReviewPage(items, items.Length, items.Average(review => review.Rating), null);
    }

    public async Task SubmitReviewAsync(string cocktailId, CocktailReview review, CancellationToken cancellationToken)
    {
        if (FailSubmissions)
        {
            throw new IOException("Simulated submission failure.");
        }

        if (SubmissionGate is not null)
        {
            await SubmissionGate.Task.WaitAsync(cancellationToken);
        }

        Submissions.Add((cocktailId, review));
    }

    public Task<IReadOnlyDictionary<string, IReadOnlyList<CocktailReview>>> GetReviewsAsync(
        IReadOnlyList<string> cocktailIds,
        CancellationToken cancellationToken)
    {
        if (FailRequests)
        {
            throw new IOException("Simulated network failure.");
        }

        Dictionary<string, IReadOnlyList<CocktailReview>> reviews = new Dictionary<string, IReadOnlyList<CocktailReview>>();
        for (int index = 0; index < cocktailIds.Count; index++)
        {
            reviews[cocktailIds[index]] = Array.AsReadOnly(
            [
                new CocktailReview("Tester A", index % 5 + 1, "A sample comment."),
                new CocktailReview("Tester B", index % 5 + 1, string.Empty)
            ]);
        }

        return Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<CocktailReview>>>(reviews);
    }
}

internal sealed class MemoryReviewerNameStore : IReviewerNameStore
{
    public string? UserName { get; private set; }

    public bool FailLoads { get; set; }

    public bool FailWrites { get; set; }

    public Task<string?> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (FailLoads)
        {
            throw new IOException("Simulated name loading failure.");
        }

        return Task.FromResult(UserName);
    }

    public Task SaveAsync(string userName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (FailWrites)
        {
            throw new IOException("Simulated name saving failure.");
        }

        UserName = userName;
        return Task.CompletedTask;
    }
}
