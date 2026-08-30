namespace AppliLeCrocodile.ViewModels;

public sealed class CocktailItemViewModel
{
    public CocktailItemViewModel(string name, string ingredients)
    {
        Name = name;
        Ingredients = ingredients;
    }

    public string Name { get; }

    public string Ingredients { get; }
}
