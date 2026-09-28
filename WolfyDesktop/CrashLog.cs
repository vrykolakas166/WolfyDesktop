using WolfyDesktop.Core.Services;

namespace WolfyDesktop;

/// <summary>
/// Appends unhandled exceptions to <c>%LocalAppData%\WolfyDesktop\crash.log</c> so that
/// crashes in release builds (which have no debugger attached) can be diagnosed.
/// </summary>
internal static class CrashLog
{
    private const long MaxSizeBytes = 512 * 1024;

    public static string FilePath { get; } = Path.Combine(new AppPaths().DataRoot, "crash.log");

    public static void Write(Exception exception, string source)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            if (File.Exists(FilePath) && new FileInfo(FilePath).Length > MaxSizeBytes)
            {
                File.Delete(FilePath);
            }

            File.AppendAllText(FilePath, $"[{DateTime.Now:O}] {source}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Nothing sensible left to do while already crashing.
        }
    }
}
