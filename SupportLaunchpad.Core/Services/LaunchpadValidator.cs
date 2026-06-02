using SupportLaunchpad.Core.Abstractions;
using SupportLaunchpad.Core.Models;

namespace SupportLaunchpad.Core.Services;

public sealed class LaunchpadValidator
{
    private readonly IFileSystem _fileSystem;

    public LaunchpadValidator(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public ValidationResult ValidateButton(LaunchpadButton button)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(button.Name))
        {
            result.AddError("Button name is required.");
        }

        if (string.IsNullOrWhiteSpace(button.Path))
        {
            result.AddError("Path or command is required.");
            return result;
        }

        switch (button.ActionType)
        {
            case LaunchActionType.Folder:
                if (!_fileSystem.DirectoryExists(button.Path))
                {
                    result.AddError("Folder path does not exist.");
                }
                break;
            case LaunchActionType.Exe:
            case LaunchActionType.PowerShell:
                if (!_fileSystem.FileExists(button.Path))
                {
                    result.AddError("File path does not exist.");
                }
                break;
            case LaunchActionType.Url:
                if (!Uri.TryCreate(button.Path, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    result.AddError("URL must be a valid HTTP or HTTPS address.");
                }
                break;
        }

        if (!string.IsNullOrWhiteSpace(button.IconPath) && !_fileSystem.FileExists(button.IconPath))
        {
            result.AddWarning("Icon path does not exist.");
        }

        return result;
    }
}
