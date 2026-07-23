using System.Text;

namespace Piootoo.Core.Services;

/// <summary>Scrive file completi tramite un temporaneo univoco nella stessa directory.</summary>
public static class AtomicFileWriter
{
    private const int MaxAttempts = 10;

    public static void WriteAllText(string path, string contents, Encoding? encoding = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new ArgumentException("Il file di destinazione non ha una directory valida.", nameof(path));
        Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(
                       tempPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.Read,
                       64 * 1024,
                       FileOptions.WriteThrough))
            using (var writer = new StreamWriter(stream, encoding ?? new UTF8Encoding(false)))
            {
                writer.Write(contents);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            CommitWithRetry(tempPath, fullPath);
        }
        finally
        {
            TryDeleteTemp(tempPath);
        }
    }

    public static void Write(string path, Action<Stream> write)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(write);
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new ArgumentException("Il file di destinazione non ha una directory valida.", nameof(path));
        Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(
                       tempPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.Read,
                       64 * 1024,
                       FileOptions.WriteThrough))
            {
                write(stream);
                stream.Flush(flushToDisk: true);
            }

            CommitWithRetry(tempPath, fullPath);
        }
        finally
        {
            TryDeleteTemp(tempPath);
        }
    }

    public static FileStream OpenReadShared(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return new FileStream(
                    Path.GetFullPath(path),
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete,
                    64 * 1024,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
            }
            catch (FileNotFoundException) when (attempt < MaxAttempts)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(Math.Min(50, 10 * attempt)));
            }
            catch (IOException ex) when (attempt < MaxAttempts && IsSharingViolation(ex))
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(Math.Min(50, 10 * attempt)));
            }
        }
    }

    private static void CommitWithRetry(string tempPath, string destinationPath)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                if (File.Exists(destinationPath))
                {
                    try
                    {
                        File.Replace(tempPath, destinationPath, null);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        File.Move(tempPath, destinationPath, overwrite: true);
                    }
                    catch (IOException) when (!File.Exists(destinationPath))
                    {
                        File.Move(tempPath, destinationPath, overwrite: false);
                    }
                }
                else
                {
                    File.Move(tempPath, destinationPath, overwrite: false);
                }

                return;
            }
            catch (IOException ex) when (attempt < MaxAttempts && IsSharingViolation(ex))
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(Math.Min(100, 25 * attempt)));
            }
        }
    }

    private static bool IsSharingViolation(IOException exception)
    {
        var errorCode = exception.HResult & 0xFFFF;
        return errorCode is 32 or 33;
    }

    private static void TryDeleteTemp(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
        catch (IOException)
        {
            // Non mascherare l'errore originale per un cleanup temporaneo fallito.
        }
        catch (UnauthorizedAccessException)
        {
            // Non mascherare l'errore originale per un cleanup temporaneo fallito.
        }
    }
}
