using System.Text;
using Avalonia.Platform;

namespace AppliLeCrocodile.Infrastructure.Assets;

public sealed class AvaloniaAssetReader : IAssetReader
{
    private const string AssetRoot = "avares://AppliLeCrocodile/";

    public Task<string> ReadTextAsync(string relativePath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        return Task.Run(async () =>
        {
            string normalizedPath = relativePath.Replace('\\', '/').TrimStart('/');
            Uri assetUri = new Uri(AssetRoot + normalizedPath, UriKind.Absolute);

            try
            {
                using Stream assetStream = AssetLoader.Open(assetUri);
                using StreamReader reader = new StreamReader(assetStream, Encoding.UTF8, true);
                return await reader.ReadToEndAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                throw new InvalidDataException($"Unable to read the application asset '{normalizedPath}'.", exception);
            }
        }, cancellationToken);
    }
}
