using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppliLeCrocodile.Configuration;
using AppliLeCrocodile.Models;
using AppliLeCrocodile.Services;

namespace AppliLeCrocodile.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ICocktailManager cocktailManager;
    private readonly ILanguageManager languageManager;
    private readonly MenuLayoutOptions layoutOptions;
    private readonly IFavoriteCocktailStore favoriteStore;
    private readonly ICocktailReviewService reviewService;
    private readonly ICocktailImageLoader imageLoader;
    private readonly IReviewerNameStore reviewerNameStore;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMenuReady))]
    private bool isLoading = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(IsMenuReady))]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private string retryText = "Réessayer";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToNextPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(GoToPreviousPageCommand))]
    private IReadOnlyList<MenuPageViewModel> pages = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToNextPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(GoToPreviousPageCommand))]
    private int currentPageIndex;

    [ObservableProperty]
    private CocktailMenuPageViewModel? cocktailPage;

    public MainViewModel(
        ILanguageManager languageManager,
        ICocktailManager cocktailManager,
        MenuLayoutOptions layoutOptions,
        IFavoriteCocktailStore favoriteStore,
        ICocktailReviewService reviewService,
        ICocktailImageLoader imageLoader,
        IReviewerNameStore reviewerNameStore)
    {
        this.languageManager = languageManager;
        this.cocktailManager = cocktailManager;
        this.layoutOptions = layoutOptions;
        this.favoriteStore = favoriteStore;
        this.reviewService = reviewService;
        this.imageLoader = imageLoader;
        this.reviewerNameStore = reviewerNameStore;
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsMenuReady => !IsLoading && !HasError && Pages.Count > 0;

    public bool IsHorizontalSwipeEnabled => CocktailPage?.IsInteractionBlocked != true;

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

    [RelayCommand]
    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            layoutOptions.Validate();
            await languageManager.InitializeAsync(LanguageManager.DefaultLanguage, cancellationToken);
            MenuCatalog catalog = await cocktailManager.LoadAsync(cancellationToken);
            if (CocktailPage is not null)
            {
                CocktailPage.PropertyChanged -= OnCocktailPagePropertyChanged;
            }

            CocktailPage = new CocktailMenuPageViewModel(
                catalog, languageManager, layoutOptions, favoriteStore, reviewService, imageLoader, reviewerNameStore);
            CocktailPage.PropertyChanged += OnCocktailPagePropertyChanged;
            await CocktailPage.LoadFavoritesAsync(cancellationToken);
            MenuPageFactory pageFactory = new MenuPageFactory(languageManager, layoutOptions);
            Pages = pageFactory.CreatePages(catalog, CocktailPage);
            CurrentPageIndex = 0;
            RetryText = languageManager.GetText("RETRY");
            IsLoading = false;
            await CocktailPage.LoadReviewsAsync(cancellationToken);
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

    private void OnCocktailPagePropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(CocktailMenuPageViewModel.IsInteractionBlocked))
        {
            OnPropertyChanged(nameof(IsHorizontalSwipeEnabled));
            GoToNextPageCommand.NotifyCanExecuteChanged();
            GoToPreviousPageCommand.NotifyCanExecuteChanged();
        }
    }
}
