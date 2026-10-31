namespace AppliLeCrocodile.Services;

public interface IReviewerNameStore
{
    Task<string?> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(string userName, CancellationToken cancellationToken);
}
