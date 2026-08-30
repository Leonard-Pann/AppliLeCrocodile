using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using AppliLeCrocodile.Infrastructure.Assets;

namespace AppliLeCrocodile.Infrastructure.Serialization;

internal sealed class JsonResourceReader
{
    private readonly IAssetReader assetReader;

    public JsonResourceReader(IAssetReader assetReader)
    {
        this.assetReader = assetReader;
    }

    public async Task<T> ReadAsync<T>(string relativePath, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken)
    {
        string json = await assetReader.ReadTextAsync(relativePath, cancellationToken);

        try
        {
            T? result = JsonSerializer.Deserialize(json, typeInfo);
            return result ?? throw new InvalidDataException($"The JSON asset '{relativePath}' contains a null root value.");
        }
        catch (JsonException exception)
        {
            string location = exception.LineNumber.HasValue
                ? $" Line {exception.LineNumber.Value + 1}, byte {exception.BytePositionInLine.GetValueOrDefault() + 1}."
                : string.Empty;
            throw new InvalidDataException($"The JSON asset '{relativePath}' is invalid.{location}", exception);
        }
    }
}
