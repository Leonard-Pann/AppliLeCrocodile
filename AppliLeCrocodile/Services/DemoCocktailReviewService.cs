using AppliLeCrocodile.Models;

namespace AppliLeCrocodile.Services;

/// <summary>
/// Provides repeatable sample reviews until a remote review service is available.
/// </summary>
public sealed class DemoCocktailReviewService : ICocktailReviewService
{
    public Task<IReadOnlyDictionary<string, IReadOnlyList<CocktailReview>>> GetReviewsAsync(
        IReadOnlyList<string> cocktailIds,
        CancellationToken cancellationToken)
    {
        return Task.Run<IReadOnlyDictionary<string, IReadOnlyList<CocktailReview>>>(() =>
        {
            Dictionary<string, IReadOnlyList<CocktailReview>> reviews = new Dictionary<string, IReadOnlyList<CocktailReview>>();
            foreach (string cocktailId in cocktailIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int seed = 17;
                foreach (char character in cocktailId)
                {
                    seed = unchecked(seed * 31 + character);
                }

                Random random = new Random(seed);
                reviews[cocktailId] = Array.AsReadOnly(Enumerable.Range(1, 3)
                    .Select(index => new CocktailReview($"Visiteur {index}", random.Next(1, 6), string.Empty)).ToArray());
            }

            return reviews;
        }, cancellationToken);
    }
}
