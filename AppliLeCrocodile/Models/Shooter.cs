namespace AppliLeCrocodile.Models;

public sealed class Shooter : IEquatable<Shooter>
{
    public Shooter(string nameId)
    {
        NameId = nameId;
    }

    public string NameId { get; }

    public bool Equals(Shooter? other)
    {
        return other is not null && StringComparer.Ordinal.Equals(NameId, other.NameId);
    }

    public override bool Equals(object? obj)
    {
        return obj is Shooter shooter && Equals(shooter);
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
