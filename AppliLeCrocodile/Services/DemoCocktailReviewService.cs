using AppliLeCrocodile.Models;
using LeCrocodile.Reviews.Contracts;

namespace AppliLeCrocodile.Services;

/// <summary>
/// Provides repeatable random sample reviews and discards submissions until a remote service is available.
/// </summary>
public sealed class DemoCocktailReviewService : ICocktailReviewService
{
    public bool IsDemo => true;

    public async Task<IReadOnlyDictionary<string, ReviewSummary>> GetSummariesAsync(
        IReadOnlyList<string> cocktailIds, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, IReadOnlyList<CocktailReview>> reviews = await GetReviewsAsync(cocktailIds, cancellationToken);
        return reviews.ToDictionary(entry => entry.Key,
            entry => new ReviewSummary(entry.Key, entry.Value.Count, entry.Value.Average(review => review.Rating)));
    }

    public async Task<ReviewPage> GetReviewPageAsync(string cocktailId, long? before, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, IReadOnlyList<CocktailReview>> reviews = await GetReviewsAsync([cocktailId], cancellationToken);
        ReviewResponse[] items = reviews[cocktailId].Select((review, index) => new ReviewResponse(
            index + 1, review.UserName, review.Rating, review.Comment, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, false))
            .ToArray();
        return new ReviewPage(items, items.Length, items.Average(review => review.Rating), null);
    }

    private static readonly string[] SampleComments =
    [
        "Un cocktail très agréable !",
        "Une belle découverte, je recommande.",
        "Un mélange original et bien équilibré.",
        "Un peu trop sucré à mon goût.",
        "À goûter au prochain passage."
    ];

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
                    .Select(index => new CocktailReview(
                        $"Visiteur {index}", random.Next(1, 6), SampleComments[random.Next(SampleComments.Length)])).ToArray());
            }

            return reviews;
        }, cancellationToken);
    }

    public Task SubmitReviewAsync(string cocktailId, CocktailReview review, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cocktailId);
        ArgumentNullException.ThrowIfNull(review);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
