using AppliLeCrocodile.Models;
using LeCrocodile.Reviews.Contracts;

namespace AppliLeCrocodile.Services;

public interface ICocktailReviewService
{
    bool IsDemo { get; }

    Task<IReadOnlyDictionary<string, ReviewSummary>> GetSummariesAsync(
        IReadOnlyList<string> cocktailIds,
        CancellationToken cancellationToken);

    Task<ReviewPage> GetReviewPageAsync(string cocktailId, long? before, CancellationToken cancellationToken);

    Task SubmitReviewAsync(string cocktailId, CocktailReview review, CancellationToken cancellationToken);
}
