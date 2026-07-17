using SupportLaunchpad.Core.Abstractions;

namespace SupportLaunchpad.Core.Services;

public sealed class SystemFileSystem : IFileSystem
{
    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(ExpandPath(path));
    }

    public bool DirectoryExists(string path)
    {
        return Directory.Exists(ExpandPath(path));
    }

    public bool FileExists(string path)
    {
        return File.Exists(ExpandPath(path));
    }

    public string GetFullPath(string path)
    {
        return Path.GetFullPath(ExpandPath(path));
    }

    public string ReadAllText(string path)
    {
        return File.ReadAllText(ExpandPath(path));
    }

    public void WriteAllText(string path, string contents)
    {
        File.WriteAllText(ExpandPath(path), contents);
    }

    public void WriteAllTextAtomic(string path, string contents)
    {
        path = ExpandPath(path);
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(temporaryPath, contents);
            File.Move(temporaryPath, path, true);
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
            catch (IOException)
            {
                // Preserve the original write failure; stale temporary files are harmless.
            }
            catch (UnauthorizedAccessException)
            {
                // Preserve the original write failure; stale temporary files are harmless.
            }
        }
    }

    public void AppendAllText(string path, string contents)
    {
        File.AppendAllText(ExpandPath(path), contents);
    }

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite)
    {
        File.Copy(ExpandPath(sourcePath), ExpandPath(destinationPath), overwrite);
    }

    private static string ExpandPath(string path) => Environment.ExpandEnvironmentVariables(path);
}
