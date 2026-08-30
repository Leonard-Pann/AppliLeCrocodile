using AppliLeCrocodile.Models;

namespace AppliLeCrocodile.Services.Filters;

public sealed class NameCocktailFilter : ICocktailFilter
{
    private readonly IReadOnlyList<string> ingredientNameIds;

    public NameCocktailFilter(IEnumerable<string> ingredientNameIds)
    {
        ArgumentNullException.ThrowIfNull(ingredientNameIds);
        this.ingredientNameIds = ingredientNameIds.ToArray();
    }

    public bool IsValidCocktail(Cocktail cocktail)
    {
        ArgumentNullException.ThrowIfNull(cocktail);

        foreach (string ingredientNameId in ingredientNameIds)
        {
            if (!cocktail.IngredientNameIds.Contains(ingredientNameId, StringComparer.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
