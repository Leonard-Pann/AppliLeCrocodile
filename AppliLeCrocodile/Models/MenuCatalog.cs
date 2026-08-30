namespace AppliLeCrocodile.Models;

public sealed class MenuCatalog
{
    public MenuCatalog(
        IReadOnlyList<Cocktail> cocktails,
        IReadOnlyList<Cocktail> softs,
        IReadOnlyList<Cocktail> fruitJuiceRecipes,
        IReadOnlyList<Ingredient> ingredients,
        IReadOnlyList<Ingredient> fruitJuices,
        IReadOnlyList<Beer> beers,
        IReadOnlyList<PricedMenuItem> snacks,
        IReadOnlyList<PricedMenuItem> sodas,
        IReadOnlyList<Shooter> shooters)
    {
        Cocktails = cocktails;
        Softs = softs;
        FruitJuiceRecipes = fruitJuiceRecipes;
        Ingredients = ingredients;
        FruitJuices = fruitJuices;
        Beers = beers;
        Snacks = snacks;
        Sodas = sodas;
        Shooters = shooters;
    }

    public IReadOnlyList<Cocktail> Cocktails { get; }

    public IReadOnlyList<Cocktail> Softs { get; }

    public IReadOnlyList<Cocktail> FruitJuiceRecipes { get; }

    public IReadOnlyList<Ingredient> Ingredients { get; }

    public IReadOnlyList<Ingredient> FruitJuices { get; }

    public IReadOnlyList<Beer> Beers { get; }

    public IReadOnlyList<PricedMenuItem> Snacks { get; }

    public IReadOnlyList<PricedMenuItem> Sodas { get; }

    public IReadOnlyList<Shooter> Shooters { get; }
}
