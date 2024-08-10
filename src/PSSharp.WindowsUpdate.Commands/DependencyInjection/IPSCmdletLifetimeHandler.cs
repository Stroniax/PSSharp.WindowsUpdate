using System.Management.Automation;

namespace PSSharp.WindowsUpdate.Commands;

public interface IPSCmdletLifetimeHandler
{
    void OnStart(PSCmdlet cmdlet);

    void OnStop(PSCmdlet cmdlet);

    void OnEnd(PSCmdlet cmdlet);
}
