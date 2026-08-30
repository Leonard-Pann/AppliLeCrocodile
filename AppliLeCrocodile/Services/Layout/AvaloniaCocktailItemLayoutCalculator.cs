using AppliLeCrocodile.Configuration;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace AppliLeCrocodile.Services.Layout;

public sealed class AvaloniaCocktailItemLayoutCalculator : ICocktailItemLayoutCalculator
{
    private readonly MenuLayoutOptions layoutOptions;
    private readonly Typeface ingredientTypeface;
    private readonly Typeface nameTypeface;

    public AvaloniaCocktailItemLayoutCalculator(MenuLayoutOptions layoutOptions)
    {
        this.layoutOptions = layoutOptions;
        FontFamily fontFamily = new FontFamily(layoutOptions.CocktailFontFamily);
        ingredientTypeface = new Typeface(fontFamily, FontStyle.Normal, FontWeight.Normal, FontStretch.Normal);
        nameTypeface = new Typeface(fontFamily, FontStyle.Normal, FontWeight.Bold, FontStretch.Normal);
    }

    public double AvailableColumnHeight => layoutOptions.CocktailColumnHeight;

    public double MeasureHeight(string name, string ingredients)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(ingredients);

        TextLayout nameLayout = CreateTextLayout(name, nameTypeface, layoutOptions.CocktailNameFontSize, double.NaN);
        TextLayout ingredientLayout = CreateTextLayout(
            ingredients,
            ingredientTypeface,
            layoutOptions.CocktailIngredientFontSize,
            layoutOptions.CocktailIngredientLineHeight);

        return Math.Ceiling(nameLayout.Height)
            + Math.Ceiling(ingredientLayout.Height)
            + layoutOptions.CocktailItemBottomSpacing;
    }

    private TextLayout CreateTextLayout(string text, Typeface typeface, double fontSize, double lineHeight)
    {
        return new TextLayout(
            text,
            typeface,
            fontSize,
            Brushes.Black,
            textWrapping: TextWrapping.Wrap,
            maxWidth: layoutOptions.CocktailColumnWidth,
            lineHeight: lineHeight);
    }
}
