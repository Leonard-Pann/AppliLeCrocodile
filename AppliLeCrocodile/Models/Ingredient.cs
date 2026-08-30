namespace AppliLeCrocodile.Models;

public sealed class Ingredient : IEquatable<Ingredient>
{
    public Ingredient(
        string nameId,
        string descriptionId,
        float alcoholVolume,
        bool isLiquid,
        bool isFruitJuice,
        string imagePath)
    {
        NameId = nameId;
        DescriptionId = descriptionId;
        AlcoholVolume = alcoholVolume;
        IsLiquid = isLiquid;
        IsFruitJuice = isFruitJuice;
        ImagePath = imagePath;
    }

    public string NameId { get; }

    public string DescriptionId { get; }

    public float AlcoholVolume { get; }

    public bool IsLiquid { get; }

    public bool IsFruitJuice { get; }

    public string ImagePath { get; }

    public bool Equals(Ingredient? other)
    {
        return other is not null && StringComparer.Ordinal.Equals(NameId, other.NameId);
    }

    public override bool Equals(object? obj)
    {
        return obj is Ingredient ingredient && Equals(ingredient);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(NameId);
    }

    public override string ToString()
    {
        return NameId;
    }

    public static bool operator ==(Ingredient? right, Ingredient? left)
    {
        return Equals(right, left);
    }

    public static bool operator !=(Ingredient? right, Ingredient? left)
    {
        return !Equals(right, left);
    }
}
