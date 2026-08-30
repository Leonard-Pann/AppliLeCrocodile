using AppliLeCrocodile.Models;

namespace AppliLeCrocodile.Services.Filters;

public interface ICocktailFilter
{
    bool IsValidCocktail(Cocktail cocktail);
}
