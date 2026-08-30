using System.Text.Json.Serialization;

namespace AppliLeCrocodile.Infrastructure.Serialization;

internal sealed class CocktailFileDto
{
    [JsonPropertyName("cocktails")]
    public CocktailDto[] Cocktails { get; init; } = [];
}

internal sealed class CocktailDto
{
    [JsonPropertyName("nameID")]
    public string NameId { get; init; } = string.Empty;

    [JsonPropertyName("imagePath")]
    public string ImagePath { get; init; } = string.Empty;

    [JsonPropertyName("ingredientsNameID")]
    public string[] IngredientNameIds { get; init; } = [];
}

internal sealed class IngredientFileDto
{
    [JsonPropertyName("ingredients")]
    public IngredientDto[] Ingredients { get; init; } = [];
}

internal sealed class IngredientDto
{
    [JsonPropertyName("nameID")]
    public string NameId { get; init; } = string.Empty;

    [JsonPropertyName("descriptionID")]
    public string DescriptionId { get; init; } = string.Empty;

    [JsonPropertyName("alcoholVolume")]
    public float AlcoholVolume { get; init; }

    [JsonPropertyName("isLiquid")]
    public bool IsLiquid { get; init; }

    [JsonPropertyName("isFruitJuice")]
    public bool IsFruitJuice { get; init; }

    [JsonPropertyName("imagePath")]
    public string ImagePath { get; init; } = string.Empty;
}

internal sealed class BeerFileDto
{
    [JsonPropertyName("beers")]
    public BeerDto[] Beers { get; init; } = [];
}

internal sealed class BeerDto
{
    [JsonPropertyName("nameID")]
    public string NameId { get; init; } = string.Empty;

    [JsonPropertyName("imagePath")]
    public string ImagePath { get; init; } = string.Empty;

    [JsonPropertyName("descriptionID")]
    public string DescriptionId { get; init; } = string.Empty;

    [JsonPropertyName("demiPriceId")]
    public string HalfPintPriceId { get; init; } = string.Empty;

    [JsonPropertyName("pintPriceId")]
    public string PintPriceId { get; init; } = string.Empty;
}

internal sealed class SnackFileDto
{
    [JsonPropertyName("snacks")]
    public PricedMenuItemDto[] Snacks { get; init; } = [];
}

internal sealed class SodaFileDto
{
    [JsonPropertyName("soda")]
    public PricedMenuItemDto[] Sodas { get; init; } = [];
}

internal sealed class PricedMenuItemDto
{
    [JsonPropertyName("nameID")]
    public string NameId { get; init; } = string.Empty;

    [JsonPropertyName("priceID")]
    public string PriceId { get; init; } = string.Empty;
}

internal sealed class ShooterFileDto
{
    [JsonPropertyName("shooters")]
    public ShooterDto[] Shooters { get; init; } = [];
}

internal sealed class ShooterDto
{
    [JsonPropertyName("nameID")]
    public string NameId { get; init; } = string.Empty;
}

internal sealed class LanguageFileDto
{
    [JsonPropertyName("language")]
    public string Language { get; init; } = string.Empty;

    [JsonPropertyName("datas")]
    public TextEntryDto[] Entries { get; init; } = [];
}

internal sealed class TextEntryDto
{
    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; init; } = string.Empty;
}

internal sealed class ConstantsFileDto
{
    [JsonPropertyName("dataConstantes")]
    public ConstantEntryDto[] Constants { get; init; } = [];
}

internal sealed class ConstantEntryDto
{
    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; init; } = string.Empty;
}
