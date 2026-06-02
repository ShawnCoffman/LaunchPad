namespace SupportLaunchpad.Core.Abstractions;

public interface IFileSystem
{
    bool FileExists(string path);

    bool DirectoryExists(string path);

    string ReadAllText(string path);

    void WriteAllText(string path, string contents);

    void CreateDirectory(string path);

    string GetFullPath(string path);
}
