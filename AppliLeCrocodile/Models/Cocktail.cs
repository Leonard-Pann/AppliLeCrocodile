namespace AppliLeCrocodile.Models;

public sealed class Cocktail : IEquatable<Cocktail>
{
    public Cocktail(string nameId, string imagePath, IReadOnlyList<string> ingredientNameIds)
    {
        NameId = nameId;
        ImagePath = imagePath;
        IngredientNameIds = ingredientNameIds;
    }

    public string NameId { get; }

    public string ImagePath { get; }

    public IReadOnlyList<string> IngredientNameIds { get; }

    public bool Equals(Cocktail? other)
    {
        return other is not null && StringComparer.Ordinal.Equals(NameId, other.NameId);
    }

    public override bool Equals(object? obj)
    {
        return obj is Cocktail cocktail && Equals(cocktail);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(NameId);
    }

    public override string ToString()
    {
        return NameId;
    }

    public static bool operator ==(Cocktail? right, Cocktail? left)
    {
        return Equals(right, left);
    }

    public static bool operator !=(Cocktail? right, Cocktail? left)
    {
        return !Equals(right, left);
    }
}
