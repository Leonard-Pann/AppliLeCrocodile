using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AppliLeCrocodile.Services;

public sealed class AssetCocktailImageLoader : ICocktailImageLoader
{
    public Task<Bitmap?> LoadAsync(string imagePath, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return null;
            }

            Uri uri = new Uri("avares://AppliLeCrocodile/Assets/Images/Cocktails/" + imagePath);
            if (!AssetLoader.Exists(uri))
            {
                return null;
            }

            using Stream stream = AssetLoader.Open(uri);
            return Bitmap.DecodeToWidth(stream, 900);
        }, cancellationToken);
    }
}
