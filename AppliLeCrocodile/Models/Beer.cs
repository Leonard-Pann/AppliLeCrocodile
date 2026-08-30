namespace AppliLeCrocodile.Models;

public sealed class Beer : IEquatable<Beer>
{
    public Beer(string nameId, string imagePath, string descriptionId, string halfPintPriceId, string pintPriceId)
    {
        NameId = nameId;
        ImagePath = imagePath;
        DescriptionId = descriptionId;
        HalfPintPriceId = halfPintPriceId;
        PintPriceId = pintPriceId;
    }

    public string NameId { get; }

    public string ImagePath { get; }

    public string DescriptionId { get; }

    public string HalfPintPriceId { get; }

    public string PintPriceId { get; }

    public bool Equals(Beer? other)
    {
        return other is not null && StringComparer.Ordinal.Equals(NameId, other.NameId);
    }

    public override bool Equals(object? obj)
    {
        return obj is Beer beer && Equals(beer);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(NameId);
    }

    public override string ToString()
    {
        return NameId;
    }
}
