namespace WolfyDesktop.Tests;

/// <summary>
/// A throwaway folder under %TEMP% that is deleted on dispose.
/// </summary>
internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WolfyDesktop.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string Combine(params string[] parts) => System.IO.Path.Combine([Path, .. parts]);

    public string CreateFile(string relativePath, byte[] content)
    {
        var fullPath = Combine(relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
        File.WriteAllBytes(fullPath, content);
        return fullPath;
    }

    public string CreateFile(string relativePath, int size = 16) => CreateFile(relativePath, new byte[size]);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // Leave it for the OS temp cleaner.
        }
    }
}
