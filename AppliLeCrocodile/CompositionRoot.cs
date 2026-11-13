using AppliLeCrocodile.Configuration;
using AppliLeCrocodile.Infrastructure.Assets;
using AppliLeCrocodile.Services;
using AppliLeCrocodile.ViewModels;

namespace AppliLeCrocodile;

internal static class CompositionRoot
{
    private static readonly System.Net.Http.HttpClient ReviewHttpClient = new System.Net.Http.HttpClient(
        new System.Net.Http.HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

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

        string favoritesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LeCrocodile", "favorites.json");
        IFavoriteCocktailStore favoriteStore = new LocalFavoriteCocktailStore(favoritesPath);
        string identityPath = Path.Combine(Path.GetDirectoryName(favoritesPath)!, "installation-id.txt");
        IInstallationIdentity identity = new LocalInstallationIdentity(identityPath);
        ICocktailReviewService reviewService = new HttpCocktailReviewService(ReviewHttpClient, identity, assetReader);
        ICocktailImageLoader imageLoader = new AssetCocktailImageLoader();
        string reviewerNamePath = Path.Combine(Path.GetDirectoryName(favoritesPath)!, "reviewer-name.json");
        IReviewerNameStore reviewerNameStore = new LocalReviewerNameStore(reviewerNamePath);
        return new MainViewModel(
            languageManager, cocktailManager, layoutOptions, favoriteStore, reviewService, imageLoader, reviewerNameStore);
    }
}
