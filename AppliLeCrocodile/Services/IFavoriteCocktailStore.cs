namespace AppliLeCrocodile.Services;

public interface IFavoriteCocktailStore
{
    Task<IReadOnlySet<string>> LoadAsync(CancellationToken cancellationToken);

    Task SetFavoriteAsync(string cocktailId, bool isFavorite, CancellationToken cancellationToken);
}
