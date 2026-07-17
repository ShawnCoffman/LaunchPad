namespace SupportLaunchpad.Core.Abstractions;

public interface IFileSystem
{
    bool FileExists(string path);

    bool DirectoryExists(string path);

    string ReadAllText(string path);

    void WriteAllText(string path, string contents);

    void WriteAllTextAtomic(string path, string contents);

    void AppendAllText(string path, string contents);

    void CopyFile(string sourcePath, string destinationPath, bool overwrite);

    void CreateDirectory(string path);

    string GetFullPath(string path);
}
