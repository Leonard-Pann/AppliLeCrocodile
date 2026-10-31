using System.Text.Json;
using AppliLeCrocodile.Infrastructure.Serialization;

namespace AppliLeCrocodile.Services;

public sealed class LocalReviewerNameStore : IReviewerNameStore
{
    private readonly string filePath;
    private readonly SemaphoreSlim storageGate = new SemaphoreSlim(1, 1);

    public LocalReviewerNameStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        this.filePath = Path.GetFullPath(filePath);
    }

    public Task<string?> LoadAsync(CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            await storageGate.WaitAsync(cancellationToken);
            try
            {
                if (!File.Exists(filePath))
                {
                    return null;
                }

                await using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                    4096, FileOptions.Asynchronous);
                ReviewerNameDto? data = await JsonSerializer.DeserializeAsync(
                    stream, JsonSerialization.Context.ReviewerNameDto, cancellationToken);
                if (data is null || string.IsNullOrWhiteSpace(data.UserName))
                {
                    throw new InvalidDataException($"The reviewer name file '{filePath}' contains an empty user name.");
                }

                return data.UserName.Trim();
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException($"The reviewer name file '{filePath}' is invalid JSON.", exception);
            }
            finally
            {
                storageGate.Release();
            }
        }, cancellationToken);
    }

    public Task SaveAsync(string userName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        return Task.Run(async () =>
        {
            await storageGate.WaitAsync(cancellationToken);
            string temporaryPath = filePath + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                ReviewerNameDto data = new ReviewerNameDto { UserName = userName.Trim() };
                await using (FileStream stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None,
                    4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
                {
                    await JsonSerializer.SerializeAsync(
                        stream, data, JsonSerialization.Context.ReviewerNameDto, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();
                File.Move(temporaryPath, filePath, true);
            }
            finally
            {
                try
                {
                    if (File.Exists(temporaryPath))
                    {
                        File.Delete(temporaryPath);
                    }
                }
                finally
                {
                    storageGate.Release();
                }
            }
        }, cancellationToken);
    }
}
