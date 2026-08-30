using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppliLeCrocodile.Infrastructure.Serialization;

internal static class JsonSerialization
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    public static MenuJsonSerializerContext Context { get; } = new MenuJsonSerializerContext(Options);

    private static JsonSerializerOptions CreateOptions()
    {
        return new JsonSerializerOptions
        {
            AllowTrailingCommas = false,
            PropertyNameCaseInsensitive = false,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };
    }
}
