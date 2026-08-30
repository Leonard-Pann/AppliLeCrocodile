namespace AppliLeCrocodile.Configuration;

/// <summary>
/// Contains the values that tune the menu composition for a phone-sized viewport.
/// </summary>
public sealed class MenuLayoutOptions
{
    public int CocktailColumnCount { get; init; } = 2;

    public double CocktailPageHorizontalPadding { get; init; } = 30d;

    public double CocktailPageTopPadding { get; init; } = 82d;

    public double CocktailPageBottomPadding { get; init; } = 30d;

    public double CocktailColumnSpacing { get; init; } = 24d;

    public double CocktailColumnBottomSafetyMargin { get; init; } = 12d;

    public double CocktailItemBottomSpacing { get; init; } = 6d;

    public double CocktailNameFontSize { get; init; } = 16d;

    public double CocktailIngredientFontSize { get; init; } = 12d;

    public double CocktailIngredientLineHeight { get; init; } = 14d;

    public string CocktailFontFamily { get; init; } =
        "avares://AppliLeCrocodile/Assets/Fonts/Balsamiq#Balsamiq Sans";

    public int SoftColumnCount { get; init; } = 2;

    public int ShooterColumnCount { get; init; } = 3;

    public int ReferenceWidth { get; init; } = 390;

    public int ReferenceHeight { get; init; } = 844;

    public double CocktailColumnWidth =>
        (ReferenceWidth - (CocktailPageHorizontalPadding * 2d) - CocktailColumnSpacing) / CocktailColumnCount;

    public double CocktailColumnHeight =>
        ReferenceHeight - CocktailPageTopPadding - CocktailPageBottomPadding - CocktailColumnBottomSafetyMargin;

    public void Validate()
    {
        if (CocktailColumnCount != 2)
        {
            throw new InvalidOperationException(
                "CocktailColumnCount must be 2 because the cocktail page has two designed columns.");
        }

        if (CocktailPageHorizontalPadding < 0d
            || CocktailPageTopPadding < 0d
            || CocktailPageBottomPadding < 0d
            || CocktailColumnSpacing < 0d
            || CocktailColumnBottomSafetyMargin < 0d
            || CocktailItemBottomSpacing < 0d)
        {
            throw new InvalidOperationException("Cocktail layout spacing values cannot be negative.");
        }

        if (CocktailNameFontSize <= 0d || CocktailIngredientFontSize <= 0d || CocktailIngredientLineHeight <= 0d)
        {
            throw new InvalidOperationException("Cocktail font and line-height values must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(CocktailFontFamily))
        {
            throw new InvalidOperationException("CocktailFontFamily cannot be empty.");
        }

        if (SoftColumnCount != 2)
        {
            throw new InvalidOperationException(
                "SoftColumnCount must be 2 because the alcohol-free page has two designed columns.");
        }

        if (ShooterColumnCount != 3)
        {
            throw new InvalidOperationException(
                "ShooterColumnCount must be 3 because the shooter section has three designed columns.");
        }

        if (ReferenceWidth <= 0 || ReferenceHeight <= 0)
        {
            throw new InvalidOperationException("Reference viewport dimensions must be greater than zero.");
        }

        if (CocktailColumnWidth <= 0d || CocktailColumnHeight <= 0d)
        {
            throw new InvalidOperationException("Cocktail layout spacing leaves no room for the menu content.");
        }
    }
}
