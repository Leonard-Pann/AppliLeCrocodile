namespace AppliLeCrocodile.Models;

public sealed class PricedMenuItem : IEquatable<PricedMenuItem>
{
    public PricedMenuItem(string nameId, string priceId)
    {
        NameId = nameId;
        PriceId = priceId;
    }

    public string NameId { get; }

    public string PriceId { get; }

    public bool Equals(PricedMenuItem? other)
    {
        return other is not null && StringComparer.Ordinal.Equals(NameId, other.NameId);
    }

    public override bool Equals(object? obj)
    {
        return obj is PricedMenuItem menuItem && Equals(menuItem);
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
