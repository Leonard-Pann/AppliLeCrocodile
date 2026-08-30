using AppliLeCrocodile.Models;

namespace AppliLeCrocodile.Services.Filters;

public sealed class PatternCocktailFilter : ICocktailFilter
{
    private readonly ILanguageManager languageManager;
    private readonly string pattern;

    public PatternCocktailFilter(ILanguageManager languageManager, string pattern)
    {
        this.languageManager = languageManager;
        this.pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
    }

    public bool IsValidCocktail(Cocktail cocktail)
    {
        ArgumentNullException.ThrowIfNull(cocktail);
        string cocktailName = languageManager.GetText(cocktail.NameId);
        return cocktailName.Contains(pattern, StringComparison.InvariantCultureIgnoreCase);
    }
}
