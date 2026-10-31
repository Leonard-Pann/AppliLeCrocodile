using System.Text.Json;
using AppliLeCrocodile.Infrastructure.Serialization;

namespace AppliLeCrocodile.Services;

public sealed class LocalFavoriteCocktailStore : IFavoriteCocktailStore
{
    private readonly string filePath;
    private readonly SemaphoreSlim storageGate = new SemaphoreSlim(1, 1);

    public LocalFavoriteCocktailStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        this.filePath = Path.GetFullPath(filePath);
    }

    public Task<IReadOnlySet<string>> LoadAsync(CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            await storageGate.WaitAsync(cancellationToken);
            try
            {
                return (IReadOnlySet<string>)await ReadFavoritesAsync(cancellationToken);
            }
            finally
            {
                storageGate.Release();
            }
        }, cancellationToken);
    }

    public Task SetFavoriteAsync(string cocktailId, bool isFavorite, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cocktailId);
        return Task.Run(async () =>
        {
            await storageGate.WaitAsync(cancellationToken);
            string temporaryPath = filePath + ".tmp";
            try
            {
                HashSet<string> favorites = await ReadFavoritesAsync(cancellationToken);
                if (isFavorite)
                {
                    favorites.Add(cocktailId);
                }
                else
                {
                    favorites.Remove(cocktailId);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                FavoritesDto data = new FavoritesDto { CocktailIds = favorites.Order(StringComparer.Ordinal).ToArray() };
                await using (FileStream stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None,
                    4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
                {
                    await JsonSerializer.SerializeAsync(stream, data, JsonSerialization.Context.FavoritesDto, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();
                File.Move(temporaryPath, filePath, true);
            }
            finally
            {
                try
                {
                    if (File.Exists(temporaryPath))
                    {
                        File.Delete(temporaryPath);
                    }
                }
                finally
                {
                    storageGate.Release();
                }
            }
        }, cancellationToken);
    }

    private async Task<HashSet<string>> ReadFavoritesAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        await using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            4096, FileOptions.Asynchronous);
        try
        {
            FavoritesDto? data = await JsonSerializer.DeserializeAsync(stream, JsonSerialization.Context.FavoritesDto, cancellationToken);
            if (data is null || data.CocktailIds is null || data.CocktailIds.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidDataException($"The favorites file '{filePath}' contains invalid cocktail identifiers.");
            }

            return data.CocktailIds.ToHashSet(StringComparer.Ordinal);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"The favorites file '{filePath}' is invalid JSON.", exception);
        }
    }
}
