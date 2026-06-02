using SupportLaunchpad.Core.Abstractions;

namespace SupportLaunchpad.Core.Services;

public sealed class SystemFileSystem : IFileSystem
{
    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public string GetFullPath(string path)
    {
        return Path.GetFullPath(path);
    }

    public string ReadAllText(string path)
    {
        return File.ReadAllText(path);
    }

    public void WriteAllText(string path, string contents)
    {
        File.WriteAllText(path, contents);
    }
}
