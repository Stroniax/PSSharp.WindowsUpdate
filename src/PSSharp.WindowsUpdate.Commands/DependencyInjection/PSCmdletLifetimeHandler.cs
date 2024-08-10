using System.Management.Automation;

namespace PSSharp.WindowsUpdate.Commands;

public abstract class PSCmdletLifetimeHandler : IPSCmdletLifetimeHandler
{
    public virtual void OnStart(PSCmdlet cmdlet) { }

    public virtual void OnStop(PSCmdlet cmdlet) { }

    public virtual void OnEnd(PSCmdlet cmdlet) { }
}
