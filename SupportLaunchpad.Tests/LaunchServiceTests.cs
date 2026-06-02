using SupportLaunchpad.Core.Models;
using SupportLaunchpad.Core.Services;

namespace SupportLaunchpad.Tests;

public sealed class LaunchServiceTests
{
    [Fact]
    public void BuildStartInfo_ConstructsPowerShellCommand()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddFile(@"C:\Scripts\demo.ps1");

        var service = new LaunchService(fileSystem, new FakeProcessStarter(), new FakeLogger(), new LaunchpadValidator(fileSystem));
        var request = new LaunchRequest
        {
            Button = new LaunchpadButton
            {
                Name = "Script",
                ActionType = LaunchActionType.PowerShell,
                Path = @"C:\Scripts\demo.ps1",
                Arguments = "-Name Test"
            },
            Settings = new LaunchpadSettings()
        };

        var startInfo = service.BuildStartInfo(request);

        Assert.Equal("powershell.exe", startInfo.FileName);
        Assert.Contains("-ExecutionPolicy Bypass", startInfo.Arguments);
        Assert.Contains("-File", startInfo.Arguments);
    }

    [Fact]
    public void BuildStartInfo_IgnoresRunAsAdmin_WhenDisabledBySettings()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddFile(@"C:\Tools\app.exe");

        var service = new LaunchService(fileSystem, new FakeProcessStarter(), new FakeLogger(), new LaunchpadValidator(fileSystem));
        var request = new LaunchRequest
        {
            Button = new LaunchpadButton
            {
                Name = "Tool",
                ActionType = LaunchActionType.Exe,
                Path = @"C:\Tools\app.exe",
                RunAsAdmin = true
            },
            Settings = new LaunchpadSettings { AllowRunAsAdmin = false }
        };

        var startInfo = service.BuildStartInfo(request);

        Assert.Equal(string.Empty, startInfo.Verb);
    }

    [Fact]
    public void Launch_FailsWhenPowerShellOutsideAllowedDirectories()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddFile(@"C:\Scripts\demo.ps1");
        var logger = new FakeLogger();
        var service = new LaunchService(fileSystem, new FakeProcessStarter(), logger, new LaunchpadValidator(fileSystem));

        var result = service.Launch(new LaunchRequest
        {
            Button = new LaunchpadButton
            {
                Name = "Script",
                ActionType = LaunchActionType.PowerShell,
                Path = @"C:\Scripts\demo.ps1"
            },
            Settings = new LaunchpadSettings
            {
                AllowedScriptDirectories = [@"D:\Approved"]
            }
        });

        Assert.False(result.Success);
        Assert.Contains("outside the allowed directories", result.ErrorMessage);
    }
}
