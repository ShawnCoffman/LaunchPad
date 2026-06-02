using System.Diagnostics;

namespace SupportLaunchpad.Core.Abstractions;

public interface IProcessStarter
{
    void Start(ProcessStartInfo startInfo);
}
