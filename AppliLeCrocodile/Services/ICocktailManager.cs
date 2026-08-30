using AppliLeCrocodile.Models;
using AppliLeCrocodile.Services.Filters;

namespace AppliLeCrocodile.Services;

public interface ICocktailManager
{
    Task<MenuCatalog> LoadAsync(CancellationToken cancellationToken);

    Ingredient GetIngredientById(string nameId);

    IReadOnlyList<Cocktail> GetCocktails(ICocktailFilter? filter);

    IReadOnlyList<Cocktail> GetSofts(ICocktailFilter? filter);

    IReadOnlyList<Ingredient> GetFruitJuices();

    IReadOnlyList<Beer> GetBeers();

    IReadOnlyList<PricedMenuItem> GetSnacks();

    IReadOnlyList<PricedMenuItem> GetSodas();

    IReadOnlyList<Shooter> GetShooters();
}
