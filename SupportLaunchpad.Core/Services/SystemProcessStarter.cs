using System.Diagnostics;
using SupportLaunchpad.Core.Abstractions;

namespace SupportLaunchpad.Core.Services;

public sealed class SystemProcessStarter : IProcessStarter
{
    public void Start(ProcessStartInfo startInfo)
    {
        Process.Start(startInfo);
    }
}
