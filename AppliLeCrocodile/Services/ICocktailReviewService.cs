using AppliLeCrocodile.Models;

namespace AppliLeCrocodile.Services;

public interface ICocktailReviewService
{
    bool IsDemo { get; }

    Task<IReadOnlyDictionary<string, IReadOnlyList<CocktailReview>>> GetReviewsAsync(
        IReadOnlyList<string> cocktailIds,
        CancellationToken cancellationToken);

    Task SubmitReviewAsync(string cocktailId, CocktailReview review, CancellationToken cancellationToken);
}
