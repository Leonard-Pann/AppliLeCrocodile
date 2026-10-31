namespace AppliLeCrocodile.Models;

public sealed record CocktailReview
{
    public CocktailReview(string userName, int rating, string comment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        if (rating is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), "A review rating must be between 1 and 5.");
        }

        UserName = userName;
        Rating = rating;
        Comment = comment ?? throw new ArgumentNullException(nameof(comment));
    }

    public string UserName { get; }

    public int Rating { get; }

    public string Comment { get; }

    public string Stars => new string('★', Rating) + new string('☆', 5 - Rating);
}
