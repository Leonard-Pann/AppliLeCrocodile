namespace AppliLeCrocodile.Infrastructure.Assets;

public interface IAssetReader
{
    Task<string> ReadTextAsync(string relativePath, CancellationToken cancellationToken);
}
