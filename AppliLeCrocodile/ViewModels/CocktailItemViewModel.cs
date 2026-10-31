using AppliLeCrocodile.Models;
using CommunityToolkit.Mvvm.Input;

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

    public IAsyncRelayCommand? OpenCommand { get; init; }

    public Cocktail? Cocktail { get; init; }
}
