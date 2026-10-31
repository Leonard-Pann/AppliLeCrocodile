using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppliLeCrocodile.Configuration;
using AppliLeCrocodile.Models;
using AppliLeCrocodile.Services;
using AppliLeCrocodile.Services.Filters;

namespace AppliLeCrocodile.ViewModels;

public sealed partial class CocktailMenuPageViewModel : MenuPageViewModel
{
    private readonly ILanguageManager languageManager;
    private readonly IFavoriteCocktailStore favoriteStore;
    private readonly ICocktailReviewService reviewService;
    private readonly ICocktailImageLoader imageLoader;
    private readonly IReviewerNameStore reviewerNameStore;
    private readonly IReadOnlyList<CocktailItemViewModel> allItems;
    private readonly IReadOnlyList<IngredientChoiceViewModel> ingredientChoices;
    private readonly HashSet<string> favoriteIds = new HashSet<string>(StringComparer.Ordinal);
    private IReadOnlyDictionary<string, IReadOnlyList<CocktailReview>> reviewsByCocktail =
        new Dictionary<string, IReadOnlyList<CocktailReview>>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSearchText))]
    private string searchText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<CocktailRowViewModel> rows = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInteractionBlocked))]
    private bool isFilterOpen;

    [ObservableProperty]
    private bool includeLowAlcohol;

    [ObservableProperty]
    private bool includeMediumAlcohol;

    [ObservableProperty]
    private bool includeStrongAlcohol;

    [ObservableProperty]
    private bool favoritesOnly;

    [ObservableProperty]
    private bool sortByRating;

    [ObservableProperty]
    private RatingFilterOption selectedRating;

    [ObservableProperty]
    private string ingredientSearchText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<IngredientChoiceViewModel> availableIngredients = [];

    [ObservableProperty]
    private IReadOnlyList<IngredientChoiceViewModel> selectedIngredients = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInteractionBlocked))]
    [NotifyPropertyChangedFor(nameof(HasSelectedCocktail))]
    private CocktailDetailViewModel? selectedCocktail;

    [ObservableProperty]
    private string favoritesStatus = string.Empty;

    [ObservableProperty]
    private string reviewStatus = "Chargement des avis…";

    [ObservableProperty]
    private bool hasReviewError;

    public CocktailMenuPageViewModel(
        MenuCatalog catalog,
        ILanguageManager languageManager,
        MenuLayoutOptions layoutOptions,
        IFavoriteCocktailStore favoriteStore,
        ICocktailReviewService reviewService,
        ICocktailImageLoader imageLoader,
        IReviewerNameStore reviewerNameStore)
        : base(languageManager.GetText("COCKTAILS_PAGE_ACCESSIBILITY"), layoutOptions)
    {
        this.languageManager = languageManager;
        this.favoriteStore = favoriteStore;
        this.reviewService = reviewService;
        this.imageLoader = imageLoader;
        this.reviewerNameStore = reviewerNameStore;
        selectedRating = RatingOptions[0];
        string cultureName = languageManager.CurrentLanguage == LanguageManager.DefaultLanguage ? "fr-FR" : "en-US";
        CompareInfo compareInfo = CultureInfo.GetCultureInfo(cultureName).CompareInfo;
        IComparer<string> comparer = Comparer<string>.Create((left, right) => compareInfo.Compare(
            left, right, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.StringSort));
        allItems = catalog.Cocktails.OrderBy(cocktail => languageManager.GetText(cocktail.NameId), comparer)
            .ThenBy(cocktail => cocktail.NameId, StringComparer.Ordinal).Select(CreateItem).ToArray();
        ingredientChoices = catalog.Ingredients
            .Select(ingredient => new IngredientChoiceViewModel(
                ingredient.NameId, languageManager.GetText(ingredient.NameId), UpdateIngredients))
            .OrderBy(ingredient => ingredient.Name, comparer).ToArray();
        UpdateIngredients();
    }

    public IReadOnlyList<RatingFilterOption> RatingOptions { get; } =
    [
        new RatingFilterOption("Toutes les notes", 0),
        new RatingFilterOption("Au moins 2 étoiles", 2),
        new RatingFilterOption("Au moins 3 étoiles", 3),
        new RatingFilterOption("Au moins 4 étoiles", 4),
        new RatingFilterOption("5 étoiles", 5)
    ];

    public bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);

    public bool HasSelectedCocktail => SelectedCocktail is not null;

    public bool IsInteractionBlocked => IsFilterOpen || HasSelectedCocktail;

    public bool HasNoResults => Rows.Count == 0;

    public int ResultCount { get; private set; }

    public string ResultText => $"{ResultCount} cocktail{(ResultCount == 1 ? string.Empty : "s")}";

    public bool HasActiveFilters => IncludeLowAlcohol || IncludeMediumAlcohol || IncludeStrongAlcohol
        || FavoritesOnly || SortByRating || SelectedRating.MinimumRating > 0 || SelectedIngredients.Count > 0;

    public string SearchPlaceholder => languageManager.GetText("SEARCH_BAR_PLACEHOLDER");

    private CocktailItemViewModel CreateItem(Cocktail cocktail)
    {
        return new CocktailItemViewModel(languageManager.GetText(cocktail.NameId),
            string.Join(", ", cocktail.IngredientNameIds.Select(languageManager.GetText)))
        {
            Cocktail = cocktail,
            OpenCommand = new AsyncRelayCommand(() => OpenCocktailAsync(cocktail))
        };
    }

    public async Task LoadFavoritesAsync(CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlySet<string> loadedIds = await favoriteStore.LoadAsync(cancellationToken);
            favoriteIds.Clear();
            favoriteIds.UnionWith(loadedIds);
            foreach (CocktailItemViewModel item in allItems)
            {
                item.IsFavorite = favoriteIds.Contains(item.Cocktail!.NameId);
            }

            FavoritesStatus = string.Empty;
            ApplyFilters();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            FavoritesStatus = "Les favoris locaux sont indisponibles. Réessayez depuis les filtres.";
        }
    }

    [RelayCommand]
    private Task RetryFavoritesAsync(CancellationToken cancellationToken)
    {
        return LoadFavoritesAsync(cancellationToken);
    }

    [RelayCommand]
    public async Task LoadReviewsAsync(CancellationToken cancellationToken)
    {
        ReviewStatus = "Chargement des avis…";
        HasReviewError = false;
        try
        {
            string[] ids = allItems.Select(item => item.Cocktail!.NameId).ToArray();
            reviewsByCocktail = await reviewService.GetReviewsAsync(ids, cancellationToken);
            ReviewStatus = string.Empty;
            ApplyFilters();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            ReviewStatus = "Les avis sont indisponibles. Réessayez depuis les filtres.";
            HasReviewError = true;
        }

        UpdateDetailReviews();
    }

    private async Task OpenCocktailAsync(Cocktail cocktail)
    {
        if (SelectedCocktail is not null)
        {
            return;
        }

        SelectedCocktail = new CocktailDetailViewModel(cocktail, languageManager.GetText(cocktail.NameId),
            cocktail.IngredientNameIds.Select(languageManager.GetText).ToArray(), favoriteIds.Contains(cocktail.NameId),
            favoriteStore, imageLoader, reviewService, reviewerNameStore, OnFavoriteChanged, CloseCocktail);
        UpdateDetailReviews();
        await SelectedCocktail.LoadImageAsync();
    }

    private void CloseCocktail()
    {
        CocktailDetailViewModel? detail = SelectedCocktail;
        SelectedCocktail = null;
        detail?.Dispose();
    }

    private void UpdateDetailReviews()
    {
        if (SelectedCocktail is null)
        {
            return;
        }

        SelectedCocktail.Reviews = reviewsByCocktail.GetValueOrDefault(SelectedCocktail.Cocktail.NameId) ?? [];
        SelectedCocktail.ReviewStatus = ReviewStatus;
    }

    private void OnFavoriteChanged(string cocktailId, bool isFavorite)
    {
        CocktailItemViewModel item = allItems.First(candidate => candidate.Cocktail!.NameId == cocktailId);
        item.IsFavorite = isFavorite;
        if (isFavorite)
        {
            favoriteIds.Add(cocktailId);
        }
        else
        {
            favoriteIds.Remove(cocktailId);
        }

        FavoritesStatus = string.Empty;
        if (FavoritesOnly)
        {
            ApplyFilters();
        }
    }

    [RelayCommand]
    private void OpenFilters()
    {
        IsFilterOpen = true;
    }

    [RelayCommand]
    private void CloseFilters()
    {
        IsFilterOpen = false;
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    [RelayCommand]
    private void ResetFilters()
    {
        SearchText = string.Empty;
        IncludeLowAlcohol = false;
        IncludeMediumAlcohol = false;
        IncludeStrongAlcohol = false;
        FavoritesOnly = false;
        SortByRating = false;
        SelectedRating = RatingOptions[0];
        IngredientSearchText = string.Empty;
        foreach (IngredientChoiceViewModel choice in ingredientChoices)
        {
            choice.IsSelected = false;
        }

        ApplyFilters();
    }

    private void UpdateIngredients()
    {
        SelectedIngredients = ingredientChoices.Where(ingredient => ingredient.IsSelected).ToArray();
        UpdateAvailableIngredients();
        ApplyFilters();
    }

    private void UpdateAvailableIngredients()
    {
        CompareInfo compareInfo = CultureInfo.GetCultureInfo("fr-FR").CompareInfo;
        AvailableIngredients = ingredientChoices.Where(ingredient => !ingredient.IsSelected
            && compareInfo.IndexOf(ingredient.Name, IngredientSearchText.Trim(),
                CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0).ToArray();
    }

    private void ApplyFilters()
    {
        TextCocktailFilter textFilter = new TextCocktailFilter(languageManager, SearchText);
        bool filterAlcohol = IncludeLowAlcohol || IncludeMediumAlcohol || IncludeStrongAlcohol;
        IEnumerable<CocktailItemViewModel> results = allItems.Where(item =>
        {
            Cocktail cocktail = item.Cocktail!;
            return textFilter.IsValidCocktail(cocktail)
                && (!FavoritesOnly || favoriteIds.Contains(cocktail.NameId))
                && SelectedIngredients.All(ingredient => cocktail.IngredientNameIds.Contains(ingredient.NameId))
                && (!filterAlcohol || cocktail.AlcoholLevel switch
                {
                    AlcoholLevel.Low => IncludeLowAlcohol,
                    AlcoholLevel.Medium => IncludeMediumAlcohol,
                    AlcoholLevel.Strong => IncludeStrongAlcohol,
                    _ => false
                })
                && (SelectedRating.MinimumRating == 0 || GetRating(cocktail.NameId) >= SelectedRating.MinimumRating);
        });

        if (SortByRating)
        {
            results = results.OrderByDescending(item => GetRating(item.Cocktail!.NameId));
        }

        CocktailItemViewModel[] items = results.ToArray();
        List<CocktailRowViewModel> resultRows = [];
        for (int index = 0; index < items.Length; index += 2)
        {
            resultRows.Add(new CocktailRowViewModel(items[index], index + 1 < items.Length ? items[index + 1] : null));
        }

        ResultCount = items.Length;
        Rows = resultRows.AsReadOnly();
        OnPropertyChanged(nameof(ResultCount));
        OnPropertyChanged(nameof(ResultText));
        OnPropertyChanged(nameof(HasNoResults));
        OnPropertyChanged(nameof(HasActiveFilters));
    }

    private double GetRating(string cocktailId)
    {
        IReadOnlyList<CocktailReview>? reviews = reviewsByCocktail.GetValueOrDefault(cocktailId);
        return reviews is { Count: > 0 } ? reviews.Average(review => review.Rating) : 0d;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    partial void OnIncludeLowAlcoholChanged(bool value) => ApplyFilters();

    partial void OnIncludeMediumAlcoholChanged(bool value) => ApplyFilters();

    partial void OnIncludeStrongAlcoholChanged(bool value) => ApplyFilters();

    partial void OnFavoritesOnlyChanged(bool value) => ApplyFilters();

    partial void OnSortByRatingChanged(bool value) => ApplyFilters();

    partial void OnSelectedRatingChanged(RatingFilterOption value) => ApplyFilters();

    partial void OnIngredientSearchTextChanged(string value) => UpdateAvailableIngredients();
}

public sealed record CocktailRowViewModel(CocktailItemViewModel Left, CocktailItemViewModel? Right)
{
    public override string ToString() => Right is null ? Left.Name : $"{Left.Name}, {Right.Name}";
}

public sealed record RatingFilterOption(string Name, int MinimumRating)
{
    public override string ToString() => Name;
}
