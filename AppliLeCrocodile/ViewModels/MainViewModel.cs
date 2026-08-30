using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppliLeCrocodile.Configuration;
using AppliLeCrocodile.Models;
using AppliLeCrocodile.Services;
using AppliLeCrocodile.Services.Filters;
using AppliLeCrocodile.Services.Layout;

namespace AppliLeCrocodile.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ICocktailManager cocktailManager;
    private readonly ICocktailItemLayoutCalculator cocktailItemLayoutCalculator;
    private readonly ILanguageManager languageManager;
    private readonly MenuLayoutOptions layoutOptions;
    private MenuPageFactory? pageFactory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMenuReady))]
    [NotifyPropertyChangedFor(nameof(IsCocktailSearchVisible))]
    [NotifyPropertyChangedFor(nameof(IsCocktailSearchResultsVisible))]
    private bool isLoading = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(IsMenuReady))]
    [NotifyPropertyChangedFor(nameof(IsCocktailSearchVisible))]
    [NotifyPropertyChangedFor(nameof(IsCocktailSearchResultsVisible))]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private string retryText = "Réessayer";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToNextPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(GoToPreviousPageCommand))]
    [NotifyPropertyChangedFor(nameof(IsCocktailSearchVisible))]
    [NotifyPropertyChangedFor(nameof(IsCocktailSearchResultsVisible))]
    private IReadOnlyList<MenuPageViewModel> pages = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToNextPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(GoToPreviousPageCommand))]
    [NotifyPropertyChangedFor(nameof(IsCocktailSearchVisible))]
    [NotifyPropertyChangedFor(nameof(IsCocktailSearchResultsVisible))]
    private int currentPageIndex;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string searchPlaceholder = string.Empty;

    [ObservableProperty]
    private string clearSearchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveCocktailSearch))]
    [NotifyPropertyChangedFor(nameof(IsCocktailSearchResultsVisible))]
    [NotifyPropertyChangedFor(nameof(IsHorizontalSwipeEnabled))]
    [NotifyCanExecuteChangedFor(nameof(ClearSearchCommand))]
    [NotifyCanExecuteChangedFor(nameof(GoToNextPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(GoToPreviousPageCommand))]
    private CocktailMenuPageViewModel? searchResultsPage;

    public MainViewModel(
        ILanguageManager languageManager,
        ICocktailManager cocktailManager,
        MenuLayoutOptions layoutOptions,
        ICocktailItemLayoutCalculator cocktailItemLayoutCalculator)
    {
        this.languageManager = languageManager;
        this.cocktailManager = cocktailManager;
        this.layoutOptions = layoutOptions;
        this.cocktailItemLayoutCalculator = cocktailItemLayoutCalculator;
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsMenuReady => !IsLoading && !HasError && Pages.Count > 0;

    public bool IsCocktailSearchVisible => IsMenuReady
        && CurrentPageIndex >= 0
        && CurrentPageIndex < Pages.Count
        && Pages[CurrentPageIndex] is CocktailMenuPageViewModel;

    public bool HasActiveCocktailSearch => SearchResultsPage is not null;

    public bool IsCocktailSearchResultsVisible => IsCocktailSearchVisible && HasActiveCocktailSearch;

    public bool IsHorizontalSwipeEnabled => !HasActiveCocktailSearch;

    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private void GoToNextPage()
    {
        CurrentPageIndex++;
    }

    private bool CanGoToNextPage()
    {
        return IsHorizontalSwipeEnabled && CurrentPageIndex >= 0 && CurrentPageIndex < Pages.Count - 1;
    }

    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private void GoToPreviousPage()
    {
        CurrentPageIndex--;
    }

    private bool CanGoToPreviousPage()
    {
        return IsHorizontalSwipeEnabled && CurrentPageIndex > 0 && CurrentPageIndex < Pages.Count;
    }

    [RelayCommand(CanExecute = nameof(CanClearSearch))]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    private bool CanClearSearch()
    {
        return HasActiveCocktailSearch;
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            layoutOptions.Validate();
            await languageManager.InitializeAsync(LanguageManager.DefaultLanguage, cancellationToken);
            MenuCatalog catalog = await cocktailManager.LoadAsync(cancellationToken);

            pageFactory = new MenuPageFactory(languageManager, layoutOptions, cocktailItemLayoutCalculator);
            Pages = pageFactory.CreatePages(catalog);
            SearchText = string.Empty;
            SearchResultsPage = null;
            SearchPlaceholder = languageManager.GetText("SEARCH_BAR_PLACEHOLDER");
            ClearSearchText = languageManager.GetText("CLEAR_SEARCH");
            CurrentPageIndex = 0;
            RetryText = languageManager.TryGetText("RETRY", out string retry) ? retry : "Réessayer";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            string title = languageManager.TryGetText("MENU_LOAD_ERROR", out string translatedTitle)
                ? translatedTitle
                : "Impossible de charger la carte.";
            ErrorMessage = $"{title}{Environment.NewLine}{exception.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyCocktailSearch(value);
    }

    private void ApplyCocktailSearch(string query)
    {
        if (pageFactory is null || Pages.Count < 4)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            SearchResultsPage = null;
            return;
        }

        TextCocktailFilter filter = new TextCocktailFilter(languageManager, query);
        IReadOnlyList<Cocktail> results = cocktailManager.GetCocktails(filter);
        SearchResultsPage = pageFactory.CreateSearchResultsPage(results);
    }
}
