using AppliLeCrocodile.Configuration;
using AppliLeCrocodile.Infrastructure.Assets;
using AppliLeCrocodile.Services;
using AppliLeCrocodile.Services.Layout;
using AppliLeCrocodile.ViewModels;

namespace AppliLeCrocodile;

internal static class CompositionRoot
{
    public static MainViewModel CreateMainViewModel()
    {
        IAssetReader assetReader = new AvaloniaAssetReader();
        ILanguageManager languageManager = new LanguageManager(assetReader);
        ICocktailManager cocktailManager = new CocktailManager(assetReader, languageManager);

        MenuLayoutOptions layoutOptions = new MenuLayoutOptions
        {
            CocktailColumnCount = 2,
            CocktailPageHorizontalPadding = 30d,
            CocktailPageTopPadding = 82d,
            CocktailPageBottomPadding = 30d,
            CocktailColumnSpacing = 24d,
            CocktailColumnBottomSafetyMargin = 12d,
            CocktailItemBottomSpacing = 6d,
            CocktailNameFontSize = 16d,
            CocktailIngredientFontSize = 12d,
            CocktailIngredientLineHeight = 14d,
            CocktailFontFamily = "avares://AppliLeCrocodile/Assets/Fonts/Balsamiq#Balsamiq Sans",
            SoftColumnCount = 2,
            ShooterColumnCount = 3,
            ReferenceWidth = 390,
            ReferenceHeight = 844
        };

        ICocktailItemLayoutCalculator cocktailItemLayoutCalculator = new AvaloniaCocktailItemLayoutCalculator(layoutOptions);
        return new MainViewModel(languageManager, cocktailManager, layoutOptions, cocktailItemLayoutCalculator);
    }
}
