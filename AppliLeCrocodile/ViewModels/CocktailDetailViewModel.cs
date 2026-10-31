using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppliLeCrocodile.Models;
using AppliLeCrocodile.Services;

namespace AppliLeCrocodile.ViewModels;

public sealed partial class CocktailDetailViewModel : ViewModelBase, IDisposable
{
    private readonly IFavoriteCocktailStore favoriteStore;
    private readonly ICocktailImageLoader imageLoader;
    private readonly Action<string, bool> favoriteChanged;
    private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
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
    [NotifyPropertyChangedFor(nameof(RatingText))]
    [NotifyPropertyChangedFor(nameof(RatingStars))]
    [NotifyPropertyChangedFor(nameof(HasReviews))]
    private IReadOnlyList<CocktailReview> reviews = [];

    [ObservableProperty]
    private string reviewStatus = string.Empty;

    public CocktailDetailViewModel(
        Cocktail cocktail,
        string name,
        IReadOnlyList<string> ingredients,
        bool isFavorite,
        IFavoriteCocktailStore favoriteStore,
        ICocktailImageLoader imageLoader,
        Action<string, bool> favoriteChanged,
        Action close)
    {
        Cocktail = cocktail;
        Name = name;
        Ingredients = ingredients;
        this.isFavorite = isFavorite;
        this.favoriteStore = favoriteStore;
        this.imageLoader = imageLoader;
        this.favoriteChanged = favoriteChanged;
        CloseCommand = new RelayCommand(close);
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

    public string RatingText => Reviews.Count == 0
        ? "Pas encore d’avis"
        : $"{Reviews.Average(review => review.Rating):0.0} / 5";

    public bool HasReviews => Reviews.Count > 0;

    public IReadOnlyList<RatingStarViewModel> RatingStars
    {
        get
        {
            if (!HasReviews)
            {
                return [];
            }

            double average = Reviews.Average(review => review.Rating);
            return Enumerable.Range(0, 5)
                .Select(index => new RatingStarViewModel(Math.Clamp(average - index, 0d, 1d) * 20d)).ToArray();
        }
    }

    public IRelayCommand CloseCommand { get; }

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
        lifetime.Cancel();
        Image?.Dispose();
        lifetime.Dispose();
    }
}

public sealed record RatingStarViewModel(double FillWidth);
