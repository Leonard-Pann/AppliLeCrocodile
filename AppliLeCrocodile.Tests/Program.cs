using System.Text.RegularExpressions;
using AppliLeCrocodile.Configuration;
using AppliLeCrocodile.Infrastructure.Assets;
using AppliLeCrocodile.Models;
using AppliLeCrocodile.Services;
using AppliLeCrocodile.Services.Filters;
using AppliLeCrocodile.ViewModels;

namespace AppliLeCrocodile.Tests;

internal static class Program
{
    private static int assertions;

    public static async Task Main()
    {
        IAssetReader reader = new FileAssetReader();
        ILanguageManager language = new LanguageManager(reader);
        await language.InitializeAsync(LanguageManager.DefaultLanguage, CancellationToken.None);
        ICocktailManager manager = new CocktailManager(reader, language);
        MenuCatalog catalog = await manager.LoadAsync(CancellationToken.None);
        MenuLayoutOptions options = new MenuLayoutOptions();
        MemoryFavoriteStore favorites = new MemoryFavoriteStore();
        FixedReviewService reviews = new FixedReviewService();
        EmptyImageLoader images = new EmptyImageLoader();
        CocktailMenuPageViewModel page = new CocktailMenuPageViewModel(catalog, language, options, favorites, reviews, images);

        Check(page.ResultCount == catalog.Cocktails.Count && page.ResultCount > 300, "Every cocktail is present.");
        Check(Flatten(page).Select(item => item.Cocktail).SequenceEqual(catalog.Cocktails),
            "Two-column reading order matches the existing alphabetical order.");
        Check(page.Rows.Take(page.Rows.Count - 1).All(row => row.Right is not null), "Only the final cell may be empty.");
        Check(!page.HasActiveFilters && !page.HasNoResults, "Initial filter state is clear.");

        page.SearchText = "  GIN  ";
        TextCocktailFilter expectedTextFilter = new TextCocktailFilter(language, "GIN");
        Check(Flatten(page).Select(item => item.Cocktail).SequenceEqual(catalog.Cocktails.Where(expectedTextFilter.IsValidCocktail)),
            "Search matches names and ingredients, ignoring case and surrounding spaces.");
        page.SearchText = "mesange";
        TextCocktailFilter accentFilter = new TextCocktailFilter(language, "mésange");
        Check(Flatten(page).Select(item => item.Cocktail).SequenceEqual(catalog.Cocktails.Where(accentFilter.IsValidCocktail)),
            "Search ignores accents.");
        page.SearchText = "a cocktail that does not exist";
        Check(page.HasNoResults && page.ResultCount == 0, "Empty search results are explicit.");
        page.ResetFiltersCommand.Execute(null);

        page.IncludeLowAlcohol = true;
        Check(Flatten(page).All(item => item.Cocktail!.AlcoholLevel == AlcoholLevel.Low), "Low alcohol filter.");
        page.IncludeStrongAlcohol = true;
        Check(page.ResultCount == catalog.Cocktails.Count(cocktail => cocktail.AlcoholLevel != AlcoholLevel.Medium),
            "Selected alcohol levels are combined with OR.");
        page.ResetFiltersCommand.Execute(null);
        page.IngredientSearchText = "gin";
        IngredientChoiceViewModel gin = page.AvailableIngredients.First(ingredient => ingredient.NameId == "GIN");
        gin.ToggleCommand.Execute(null);
        Check(page.SelectedIngredients.Contains(gin) && !page.AvailableIngredients.Contains(gin), "Ingredient selection becomes a chip.");
        Check(Flatten(page).All(item => item.Cocktail!.IngredientNameIds.Contains("GIN")), "Selected ingredient filters cocktails.");
        page.IngredientSearchText = "citron";
        IngredientChoiceViewModel lemon = page.AvailableIngredients.First(ingredient => ingredient.NameId == "CITRON");
        lemon.ToggleCommand.Execute(null);
        Check(page.ResultCount == catalog.Cocktails.Count(cocktail => cocktail.IngredientNameIds.Contains("GIN")
            && cocktail.IngredientNameIds.Contains("CITRON")), "Multiple ingredients are combined with AND.");
        page.IncludeMediumAlcohol = true;
        page.SearchText = "gin";
        Check(Flatten(page).All(item => item.Cocktail!.AlcoholLevel == AlcoholLevel.Medium
            && item.Cocktail.IngredientNameIds.Contains("CITRON")), "Text, alcohol and ingredient filters compose.");
        page.ResetFiltersCommand.Execute(null);
        Check(!page.HasActiveFilters && page.SearchText.Length == 0 && page.SelectedIngredients.Count == 0
            && page.ResultCount == catalog.Cocktails.Count, "Reset restores the entire catalogue and every filter.");

        await page.LoadReviewsCommand.ExecuteAsync(null);
        page.SelectedRating = page.RatingOptions.Single(option => option.MinimumRating == 4);
        Check(Flatten(page).All(item => Array.IndexOf(catalog.Cocktails.ToArray(), item.Cocktail) % 5 >= 3),
            "Rating threshold uses the global average.");
        page.SelectedRating = page.RatingOptions[0];
        page.SortByRating = true;
        int[] scores = Flatten(page).Select(item => Array.IndexOf(catalog.Cocktails.ToArray(), item.Cocktail) % 5 + 1).ToArray();
        Check(scores.SequenceEqual(scores.OrderDescending()), "Highest-rated cocktails appear first.");
        CocktailItemViewModel[] sortedItems = Flatten(page);
        Check(sortedItems.Where(item => Array.IndexOf(catalog.Cocktails.ToArray(), item.Cocktail) % 5 == 4)
            .Select(item => item.Cocktail).SequenceEqual(catalog.Cocktails.Where((cocktail, index) => index % 5 == 4)),
            "Rating ties retain alphabetical order.");
        page.ResetFiltersCommand.Execute(null);

        CocktailItemViewModel first = page.Rows[0].Left;
        await first.OpenCommand!.ExecuteAsync(null);
        CocktailDetailViewModel detail = page.SelectedCocktail ?? throw new InvalidOperationException("Detail did not open.");
        Check(page.IsInteractionBlocked && detail.Cocktail == first.Cocktail, "Opening a cocktail blocks horizontal page navigation.");
        Check(detail.Reviews.Count == 2 && detail.Reviews[0].UserName == "Tester A"
            && detail.Reviews[0].Comment.Length > 0, "Detail exposes ratings, comments and user names.");
        Check(detail.HasNoImage && detail.Ingredients.Count > 0, "Missing images do not prevent displaying ingredients.");
        IReadOnlyList<CocktailRowViewModel> originalRows = page.Rows;
        await detail.ToggleFavoriteCommand.ExecuteAsync(null);
        Check(detail.IsFavorite, "A cocktail can be favorited.");
        Check(ReferenceEquals(originalRows, page.Rows), "Favoriting preserves the unfiltered list and its scroll position.");
        detail.CloseCommand.Execute(null);
        Check(!page.IsInteractionBlocked && page.SelectedCocktail is null, "Back closes the detail.");
        page.FavoritesOnly = true;
        Check(page.ResultCount == 1 && page.Rows[0].Left.Cocktail == first.Cocktail, "Favorites-only filter.");
        await first.OpenCommand.ExecuteAsync(null);
        detail = page.SelectedCocktail!;
        Check(detail.IsFavorite, "Favorite state survives reopening a detail.");
        favorites.FailWrites = true;
        await detail.ToggleFavoriteCommand.ExecuteAsync(null);
        Check(detail.IsFavorite && detail.ErrorMessage.Length > 0, "A failed save keeps the previous favorite state.");
        favorites.FailWrites = false;
        await detail.ToggleFavoriteCommand.ExecuteAsync(null);
        Check(!detail.IsFavorite && page.ResultCount == 0, "Unfavoriting immediately updates filtered results.");
        detail.CloseCommand.Execute(null);
        page.ResetFiltersCommand.Execute(null);
        page.OpenFiltersCommand.Execute(null);
        Check(page.IsInteractionBlocked, "Opening filters blocks page navigation.");
        page.CloseFiltersCommand.Execute(null);
        Check(!page.IsInteractionBlocked, "Closing filters restores navigation.");

        reviews.FailRequests = true;
        await page.LoadReviewsCommand.ExecuteAsync(null);
        Check(page.HasReviewError && page.ResultCount == catalog.Cocktails.Count, "Review failures preserve the menu.");
        reviews.FailRequests = false;
        await page.LoadReviewsCommand.ExecuteAsync(null);
        Check(!page.HasReviewError, "Review loading can be retried.");

        MainViewModel main = new MainViewModel(language, manager, options, favorites, reviews, images);
        await main.InitializeCommand.ExecuteAsync(null);
        Check(main.IsMenuReady && main.Pages.Count == 5 && main.Pages[2] is CocktailMenuPageViewModel
            && main.Pages[3] is SoftPageViewModel && main.Pages[4] is LastPageViewModel, "The menu contains exactly five sections.");
        Check(!main.GoToPreviousPageCommand.CanExecute(null), "Cannot navigate before the cover.");
        main.GoToNextPageCommand.Execute(null);
        main.GoToNextPageCommand.Execute(null);
        main.CocktailPage!.OpenFiltersCommand.Execute(null);
        Check(!main.GoToNextPageCommand.CanExecute(null) && !main.GoToPreviousPageCommand.CanExecute(null),
            "Navigation commands respect modal state.");
        main.CocktailPage.CloseFiltersCommand.Execute(null);
        main.GoToNextPageCommand.Execute(null);
        main.GoToNextPageCommand.Execute(null);
        Check(main.CurrentPageIndex == 4 && !main.GoToNextPageCommand.CanExecute(null), "Last-page boundary is enforced.");

        await VerifyStorageAsync();
        await VerifyValidationAsync(language);
        DemoCocktailReviewService demo = new DemoCocktailReviewService();
        IReadOnlyDictionary<string, IReadOnlyList<CocktailReview>> sample = await demo.GetReviewsAsync(["ABBAYE"], CancellationToken.None);
        IReadOnlyDictionary<string, IReadOnlyList<CocktailReview>> repeat = await demo.GetReviewsAsync(["ABBAYE"], CancellationToken.None);
        Check(sample["ABBAYE"].SequenceEqual(repeat["ABBAYE"])
            && sample["ABBAYE"].All(review => review.Rating is >= 1 and <= 5 && review.Comment.Length == 0),
            "Demo reviews are stable, valid and have empty comments.");
        Console.WriteLine($"Passed {assertions} checks.");
    }

    private static CocktailItemViewModel[] Flatten(CocktailMenuPageViewModel page)
    {
        return page.Rows.SelectMany(row => row.Right is null ? new[] { row.Left } : new[] { row.Left, row.Right }).ToArray();
    }

    private static async Task VerifyStorageAsync()
    {
        string directory = Path.Combine(Path.GetTempPath(), "LeCrocodileTests", Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "favorites.json");
        try
        {
            LocalFavoriteCocktailStore store = new LocalFavoriteCocktailStore(path);
            Check((await store.LoadAsync(CancellationToken.None)).Count == 0, "Missing favorites file means no favorites.");
            await Task.WhenAll(store.SetFavoriteAsync("ABBAYE", true, CancellationToken.None),
                store.SetFavoriteAsync("ALASKA", true, CancellationToken.None));
            LocalFavoriteCocktailStore reopened = new LocalFavoriteCocktailStore(path);
            Check((await reopened.LoadAsync(CancellationToken.None)).SetEquals(["ABBAYE", "ALASKA"]),
                "Concurrent saves survive a new store instance.");
            await reopened.SetFavoriteAsync("ABBAYE", false, CancellationToken.None);
            Check((await store.LoadAsync(CancellationToken.None)).SetEquals(["ALASKA"]), "Removing a favorite persists.");
            await File.WriteAllTextAsync(path, "invalid JSON");
            await ExpectAsync<InvalidDataException>(() => store.SetFavoriteAsync("ABBAYE", true, CancellationToken.None));
            Check(await File.ReadAllTextAsync(path) == "invalid JSON", "Invalid storage is reported without overwriting user data.");
            await File.WriteAllTextAsync(path, "{\"CocktailIds\":[\"ALASKA\"]}");
            Check((await store.LoadAsync(CancellationToken.None)).Contains("ALASKA"), "Storage gate is released after an error.");
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                File.Delete(path);
                Directory.Delete(directory);
            }
        }
    }

    private static async Task VerifyValidationAsync(ILanguageManager language)
    {
        OverrideAssetReader invalidReader = new OverrideAssetReader(json =>
            Regex.Replace(json, "\"alcoholLevel\": [123]", "\"alcoholLevel\": 7"));
        CocktailManager invalid = new CocktailManager(invalidReader, language);
        await ExpectAsync<InvalidDataException>(() => invalid.LoadAsync(CancellationToken.None));
        OverrideAssetReader missingReader = new OverrideAssetReader(json =>
            Regex.Replace(json, "\"alcoholLevel\": [123],", string.Empty));
        CocktailManager missing = new CocktailManager(missingReader, language);
        await ExpectAsync<InvalidDataException>(() => missing.LoadAsync(CancellationToken.None));
        Check(true, "Missing or invalid alcohol levels are rejected by catalogue validation.");
    }

    private static async Task ExpectAsync<TException>(Func<Task> action) where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException)
        {
            assertions++;
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }

        assertions++;
    }
}
