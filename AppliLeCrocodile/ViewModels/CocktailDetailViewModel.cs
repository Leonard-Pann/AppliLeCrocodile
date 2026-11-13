using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppliLeCrocodile.Models;
using AppliLeCrocodile.Services;
using LeCrocodile.Reviews.Contracts;

namespace AppliLeCrocodile.ViewModels;

public sealed partial class CocktailDetailViewModel : ViewModelBase, IDisposable
{
    private readonly IFavoriteCocktailStore favoriteStore;
    private readonly ICocktailImageLoader imageLoader;
    private readonly ICocktailReviewService reviewService;
    private readonly IReviewerNameStore reviewerNameStore;
    private readonly Action close;
    private readonly Action<string, bool> favoriteChanged;
    private readonly Action<ReviewSummary>? reviewsChanged;
    private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
    private long? nextBefore;
    private bool isDisposed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteActionText))]
    private bool isFavorite;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoImage))]
    private Bitmap? image;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<CocktailReview> reviews = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RatingText))]
    [NotifyPropertyChangedFor(nameof(RatingStars))]
    [NotifyPropertyChangedFor(nameof(HasReviews))]
    private long totalReviewCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RatingText))]
    [NotifyPropertyChangedFor(nameof(RatingStars))]
    private double averageRating;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshReviewsCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadMoreReviewsCommand))]
    private bool isLoadingReviews;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadMoreReviewsCommand))]
    private bool hasMoreReviews;

    [ObservableProperty]
    private bool hasReviewError;

    [ObservableProperty]
    private string reviewStatus = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasReviewComposer))]
    private ReviewComposerViewModel? reviewComposer;

    [ObservableProperty]
    private string submissionStatus = string.Empty;

    public CocktailDetailViewModel(
        Cocktail cocktail,
        string name,
        IReadOnlyList<string> ingredients,
        bool isFavorite,
        IFavoriteCocktailStore favoriteStore,
        ICocktailImageLoader imageLoader,
        ICocktailReviewService reviewService,
        IReviewerNameStore reviewerNameStore,
        Action<string, bool> favoriteChanged,
        Action close,
        Action<ReviewSummary>? reviewsChanged = null)
    {
        Cocktail = cocktail;
        Name = name;
        Ingredients = ingredients;
        this.isFavorite = isFavorite;
        this.favoriteStore = favoriteStore;
        this.imageLoader = imageLoader;
        this.reviewService = reviewService;
        this.reviewerNameStore = reviewerNameStore;
        this.close = close;
        this.favoriteChanged = favoriteChanged;
        this.reviewsChanged = reviewsChanged;
        CloseCommand = new RelayCommand(Close);
    }

    public Cocktail Cocktail { get; }

    public string Name { get; }

    public IReadOnlyList<string> Ingredients { get; }

    public string Description => string.IsNullOrWhiteSpace(Cocktail.Description) ? "Description à venir." : Cocktail.Description;

    public string AlcoholText => Cocktail.AlcoholLevel switch
    {
        AlcoholLevel.Low => "Alcool : faible",
        AlcoholLevel.Medium => "Alcool : moyen",
        AlcoholLevel.Strong => "Alcool : chargé",
        _ => string.Empty
    };

    public bool HasNoImage => Image is null;

    public string FavoriteActionText => IsFavorite ? "Retirer des favoris" : "Ajouter aux favoris";

    public string RatingText => TotalReviewCount == 0
        ? "Pas encore d’avis"
        : $"{AverageRating:0.0} / 5";

    public bool HasReviews => TotalReviewCount > 0;

    public IReadOnlyList<RatingStarViewModel> RatingStars
    {
        get
        {
            if (!HasReviews)
            {
                return [];
            }

            double average = AverageRating;
            return Enumerable.Range(0, 5)
                .Select(index => new RatingStarViewModel(Math.Clamp(average - index, 0d, 1d) * 20d)).ToArray();
        }
    }

    public IRelayCommand CloseCommand { get; }

    public bool HasReviewComposer => ReviewComposer is not null;

    public bool IsDemo => reviewService.IsDemo;

    [RelayCommand]
    private async Task OpenReviewComposerAsync()
    {
        if (ReviewComposer is not null || isDisposed)
        {
            return;
        }

        SubmissionStatus = string.Empty;
        ReviewComposer = new ReviewComposerViewModel(
            Cocktail.NameId, reviewService, reviewerNameStore, OnReviewSubmittedAsync, CloseReviewComposer);
        await ReviewComposer.LoadNameAsync();
    }

    private async Task OnReviewSubmittedAsync()
    {
        CloseReviewComposer();
        SubmissionStatus = reviewService.IsDemo
            ? "Merci ! En mode démonstration, cet avis n’est pas conservé."
            : "Merci ! Votre avis a été enregistré. Un nouvel envoi remplacera votre avis sur ce cocktail.";
        Task? pendingRefresh = RefreshReviewsCommand.ExecutionTask;
        Task? pendingPage = LoadMoreReviewsCommand.ExecutionTask;
        if (pendingRefresh is not null)
        {
            await pendingRefresh;
        }

        if (pendingPage is not null)
        {
            await pendingPage;
        }

        if (!isDisposed)
        {
            await RefreshReviewsCommand.ExecuteAsync(null);
        }
    }

    private bool CanRefreshReviews() => !isDisposed && !IsLoadingReviews;

    private bool CanLoadMoreReviews() => CanRefreshReviews() && HasMoreReviews;

    [RelayCommand(CanExecute = nameof(CanRefreshReviews))]
    private Task RefreshReviewsAsync() => LoadReviewsAsync(false);

    [RelayCommand(CanExecute = nameof(CanLoadMoreReviews))]
    private Task LoadMoreReviewsAsync() => LoadReviewsAsync(true);

    private async Task LoadReviewsAsync(bool append)
    {
        if (!CanRefreshReviews())
        {
            return;
        }

        IsLoadingReviews = true;
        HasReviewError = false;
        ReviewStatus = "Chargement des avis…";
        try
        {
            ReviewPage page = await reviewService.GetReviewPageAsync(Cocktail.NameId, append ? nextBefore : null, lifetime.Token);
            if (isDisposed)
            {
                return;
            }

            CocktailReview[] loaded = page.Items.Select(review => new CocktailReview(
                review.UserName, review.Rating, review.Comment)).ToArray();
            Reviews = append ? Reviews.Concat(loaded).ToArray() : loaded;
            TotalReviewCount = page.TotalCount;
            AverageRating = page.AverageRating;
            nextBefore = page.NextBefore;
            HasMoreReviews = nextBefore is not null;
            ReviewStatus = TotalReviewCount == 0 ? "Soyez le premier à donner votre avis." : string.Empty;
            reviewsChanged?.Invoke(new ReviewSummary(Cocktail.NameId, TotalReviewCount, AverageRating));
        }
        catch (OperationCanceledException) when (isDisposed)
        {
        }
        catch (Exception)
        {
            if (!isDisposed)
            {
                HasReviewError = true;
                ReviewStatus = "Impossible de charger les avis. Vérifiez votre connexion puis réessayez.";
            }
        }
        finally
        {
            IsLoadingReviews = false;
        }
    }

    private void CloseReviewComposer()
    {
        ReviewComposerViewModel? composer = ReviewComposer;
        ReviewComposer = null;
        composer?.Dispose();
    }

    private void Close()
    {
        if (ReviewComposer is not null)
        {
            if (ReviewComposer.CancelCommand.CanExecute(null))
            {
                ReviewComposer.CancelCommand.Execute(null);
            }

            return;
        }

        close();
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        ErrorMessage = string.Empty;
        try
        {
            bool newValue = !IsFavorite;
            await favoriteStore.SetFavoriteAsync(Cocktail.NameId, newValue, lifetime.Token);
            favoriteChanged(Cocktail.NameId, newValue);
            if (!isDisposed)
            {
                IsFavorite = newValue;
            }
        }
        catch (OperationCanceledException) when (isDisposed)
        {
        }
        catch (Exception)
        {
            ErrorMessage = "Impossible d’enregistrer ce favori. Réessayez en touchant l’étoile.";
        }
    }

    public async Task LoadImageAsync()
    {
        try
        {
            Bitmap? loadedImage = await imageLoader.LoadAsync(Cocktail.ImagePath, lifetime.Token);
            if (isDisposed)
            {
                loadedImage?.Dispose();
                return;
            }

            Image = loadedImage;
        }
        catch (OperationCanceledException) when (isDisposed)
        {
        }
        catch (Exception)
        {
            if (!isDisposed)
            {
                ErrorMessage = "La photo de ce cocktail est indisponible.";
            }
        }
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        RefreshReviewsCommand.NotifyCanExecuteChanged();
        LoadMoreReviewsCommand.NotifyCanExecuteChanged();
        CloseReviewComposer();
        lifetime.Cancel();
        Image?.Dispose();
        lifetime.Dispose();
    }
}

public sealed record RatingStarViewModel(double FillWidth);
