using AppliLeCrocodile.Models;

namespace AppliLeCrocodile.Services;

public interface ICocktailReviewService
{
    Task<IReadOnlyDictionary<string, IReadOnlyList<CocktailReview>>> GetReviewsAsync(
        IReadOnlyList<string> cocktailIds,
        CancellationToken cancellationToken);
}
