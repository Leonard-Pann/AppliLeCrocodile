using Avalonia.Media.Imaging;
using AppliLeCrocodile.Infrastructure.Assets;
using AppliLeCrocodile.Models;
using AppliLeCrocodile.Services;

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
    public bool FailRequests { get; set; }

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
