using System.Text;

namespace AppliLeCrocodile.Services;

public sealed class LocalInstallationIdentity : IInstallationIdentity
{
    private readonly string filePath;
    private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
    private Guid? cachedId;

    public LocalInstallationIdentity(string filePath)
    {
        this.filePath = Path.GetFullPath(filePath);
    }

    public Task<Guid> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (cachedId is Guid existingId)
                {
                    return existingId;
                }

                if (!File.Exists(filePath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                    Guid newId = Guid.NewGuid();
                    string temporaryPath = filePath + "." + newId.ToString("N") + ".tmp";
                    try
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(newId.ToString("D"));
                        await using (FileStream stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write,
                            FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
                        {
                            await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                        }

                        cancellationToken.ThrowIfCancellationRequested();
                        try
                        {
                            File.Move(temporaryPath, filePath);
                        }
                        catch (IOException) when (File.Exists(filePath))
                        {
                            // Another instance created the installation identity first. Read that identity below.
                        }
                    }
                    finally
                    {
                        File.Delete(temporaryPath);
                    }
                }

                string text = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
                if (!Guid.TryParseExact(text.Trim(), "D", out Guid identity) || identity == Guid.Empty)
                {
                    throw new InvalidDataException("The installation identity file does not contain a valid nonempty UUID.");
                }

                cachedId = identity;
                return identity;
            }
            finally
            {
                gate.Release();
            }
        }, cancellationToken);
    }
}
