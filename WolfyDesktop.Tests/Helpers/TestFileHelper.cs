namespace WolfyDesktop.Tests.Helpers;

/// <summary>
/// Helper class for creating temporary test files and directories
/// </summary>
public class TestFileHelper : IDisposable
{
    private readonly List<string> _createdFiles = new();
    private readonly List<string> _createdDirectories = new();

    public string CreateTestDirectory(string basePath, string directoryName)
    {
        var fullPath = Path.Combine(basePath, directoryName);
        
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
            _createdDirectories.Add(fullPath);
        }

        return fullPath;
    }

    public string CreateTestFile(string directory, string fileName, string content = "")
    {
        var fullPath = Path.Combine(directory, fileName);
        File.WriteAllText(fullPath, content);
        _createdFiles.Add(fullPath);
        return fullPath;
    }

    public string CreateTestFile(string directory, string fileName, byte[] content)
    {
        var fullPath = Path.Combine(directory, fileName);
        File.WriteAllBytes(fullPath, content);
        _createdFiles.Add(fullPath);
        return fullPath;
    }

    public void Dispose()
    {
        foreach (var file in _createdFiles.Where(File.Exists))
        {
            try { File.Delete(file); } catch { }
        }

        foreach (var directory in _createdDirectories.Where(Directory.Exists))
        {
            try { Directory.Delete(directory, true); } catch { }
        }
    }
}
