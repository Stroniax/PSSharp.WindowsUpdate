using System.Management.Automation;

namespace PSSharp.WindowsUpdate.Commands;

public sealed class PSCmdletAccessorLifetimeHandler(IPSCmdletAccessor accessor)
    : PSCmdletLifetimeHandler
{
    private readonly IPSCmdletAccessor _accessor = accessor;

    public override void OnStart(PSCmdlet cmdlet)
    {
        if (_accessor is PSCmdletAccessor accessor)
        {
            accessor.Cmdlet = cmdlet;
        }
    }

    public override void OnStop(PSCmdlet cmdlet)
    {
        if (_accessor is PSCmdletAccessor accessor)
        {
            accessor.Cmdlet = null;
        }
    }

    public override void OnEnd(PSCmdlet cmdlet)
    {
        if (_accessor is PSCmdletAccessor accessor)
        {
            accessor.Cmdlet = null;
        }
    }
}
