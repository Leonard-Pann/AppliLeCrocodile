using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppliLeCrocodile.Models;
using AppliLeCrocodile.Services;

namespace AppliLeCrocodile.ViewModels;

public sealed partial class ReviewComposerViewModel : ViewModelBase, IDisposable
{
    private readonly string cocktailId;
    private readonly ICocktailReviewService reviewService;
    private readonly IReviewerNameStore nameStore;
    private readonly Func<Task> submitted;
    private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
    private bool isDisposed;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string userName = string.Empty;

    [ObservableProperty]
    private bool isUserNameRequired = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string comment = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private int rating;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private bool isLoading = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool isSubmitting;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private bool hasLoadError;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public ReviewComposerViewModel(
        string cocktailId,
        ICocktailReviewService reviewService,
        IReviewerNameStore nameStore,
        Func<Task> submitted,
        Action cancel)
    {
        this.cocktailId = cocktailId;
        this.reviewService = reviewService;
        this.nameStore = nameStore;
        this.submitted = submitted;
        CancelCommand = new RelayCommand(cancel, () => !IsSubmitting);
        RatingChoices = Enumerable.Range(1, 5).Select(value => new ReviewStarChoiceViewModel(value, SelectRating)).ToArray();
    }

    public IReadOnlyList<ReviewStarChoiceViewModel> RatingChoices { get; }

    public bool IsBusy => IsLoading || IsSubmitting;

    public bool IsDemo => reviewService.IsDemo;

    public IRelayCommand CancelCommand { get; }

    [RelayCommand]
    public async Task LoadNameAsync()
    {
        IsLoading = true;
        HasLoadError = false;
        ErrorMessage = string.Empty;
        try
        {
            string? savedName = await nameStore.LoadAsync(lifetime.Token);
            if (!isDisposed)
            {
                UserName = savedName ?? UserName;
                IsUserNameRequired = savedName is null;
            }
        }
        catch (OperationCanceledException) when (isDisposed)
        {
        }
        catch (Exception)
        {
            HasLoadError = true;
            ErrorMessage = "Impossible de lire le pseudo enregistré. Réessayez avant d’envoyer votre avis.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SelectRating(int value)
    {
        Rating = value;
    }

    partial void OnRatingChanged(int value)
    {
        foreach (ReviewStarChoiceViewModel choice in RatingChoices)
        {
            choice.IsFilled = choice.Value <= value;
        }
    }

    private bool CanSubmit()
    {
        return !isDisposed && !IsBusy && !HasLoadError && Rating is >= 1 and <= 5
            && !string.IsNullOrWhiteSpace(UserName) && UserName.Trim().Length <= 40
            && !UserName.Any(char.IsControl)
            && !string.IsNullOrWhiteSpace(Comment) && Comment.Trim().Length <= 2000
            && !Comment.Any(character => char.IsControl(character) && character is not '\r' and not '\n' and not '\t');
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        if (!CanSubmit())
        {
            return;
        }

        IsSubmitting = true;
        ErrorMessage = string.Empty;
        try
        {
            string author = UserName.Trim();
            if (IsUserNameRequired)
            {
                await nameStore.SaveAsync(author, lifetime.Token);
                UserName = author;
                IsUserNameRequired = false;
            }

            CocktailReview review = new CocktailReview(author, Rating, Comment.Trim());
            await reviewService.SubmitReviewAsync(cocktailId, review, lifetime.Token);
            if (!isDisposed)
            {
                await submitted();
            }
        }
        catch (OperationCanceledException) when (isDisposed)
        {
        }
        catch (Exception)
        {
            ErrorMessage = "Impossible d’envoyer votre avis. Vos champs sont conservés, vous pouvez réessayer.";
        }
        finally
        {
            IsSubmitting = false;
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
        lifetime.Dispose();
        SubmitCommand.NotifyCanExecuteChanged();
    }
}

public sealed partial class ReviewStarChoiceViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool isFilled;

    public ReviewStarChoiceViewModel(int value, Action<int> select)
    {
        Value = value;
        SelectCommand = new RelayCommand(() => select(value));
    }

    public int Value { get; }

    public string AccessibleName => $"{Value} étoile{(Value == 1 ? string.Empty : "s")} sur 5";

    public IRelayCommand SelectCommand { get; }
}
