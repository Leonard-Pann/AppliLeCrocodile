namespace AppliLeCrocodile.Services;

public interface IInstallationIdentity
{
    Task<Guid> GetOrCreateAsync(CancellationToken cancellationToken);
}
