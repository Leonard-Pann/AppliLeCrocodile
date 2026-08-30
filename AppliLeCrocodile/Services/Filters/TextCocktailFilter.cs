using System.Globalization;
using System.Text;
using AppliLeCrocodile.Models;

namespace AppliLeCrocodile.Services.Filters;

public sealed class TextCocktailFilter : ICocktailFilter
{
    private readonly ILanguageManager languageManager;
    private readonly string normalizedQuery;

    public TextCocktailFilter(ILanguageManager languageManager, string query)
    {
        this.languageManager = languageManager;
        normalizedQuery = Normalize(query ?? throw new ArgumentNullException(nameof(query)));
    }

    public bool IsValidCocktail(Cocktail cocktail)
    {
        ArgumentNullException.ThrowIfNull(cocktail);

        if (Normalize(languageManager.GetText(cocktail.NameId)).Contains(normalizedQuery, StringComparison.Ordinal))
        {
            return true;
        }

        foreach (string ingredientNameId in cocktail.IngredientNameIds)
        {
            string ingredientName = languageManager.GetText(ingredientNameId);
            if (Normalize(ingredientName).Contains(normalizedQuery, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string Normalize(string value)
    {
        string decomposedValue = value.Trim().Normalize(NormalizationForm.FormD);
        StringBuilder normalizedValue = new StringBuilder(decomposedValue.Length);

        foreach (char character in decomposedValue)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                normalizedValue.Append(char.ToUpperInvariant(character));
            }
        }

        return normalizedValue.ToString().Normalize(NormalizationForm.FormC);
    }
}
