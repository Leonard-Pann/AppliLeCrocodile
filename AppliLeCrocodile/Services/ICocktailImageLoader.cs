using Avalonia.Media.Imaging;

namespace AppliLeCrocodile.Services;

public interface ICocktailImageLoader
{
    Task<Bitmap?> LoadAsync(string imagePath, CancellationToken cancellationToken);
}
