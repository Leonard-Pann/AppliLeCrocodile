using System.Text.Json.Serialization;

namespace AppliLeCrocodile.Infrastructure.Serialization;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(CocktailFileDto))]
[JsonSerializable(typeof(IngredientFileDto))]
[JsonSerializable(typeof(BeerFileDto))]
[JsonSerializable(typeof(SnackFileDto))]
[JsonSerializable(typeof(SodaFileDto))]
[JsonSerializable(typeof(ShooterFileDto))]
[JsonSerializable(typeof(LanguageFileDto))]
[JsonSerializable(typeof(ConstantsFileDto))]
internal sealed partial class MenuJsonSerializerContext : JsonSerializerContext
{
}
