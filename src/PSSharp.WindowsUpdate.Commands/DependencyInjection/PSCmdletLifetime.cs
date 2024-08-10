using System.Management.Automation;

namespace PSSharp.WindowsUpdate.Commands;

internal sealed class PSCmdletLifetime(IEnumerable<IPSCmdletLifetimeHandler> handlers)
    : IPSCmdletLifetime
{
    private readonly IEnumerable<IPSCmdletLifetimeHandler> _handlers = handlers;

    public void OnStart(PSCmdlet cmdlet)
    {
        foreach (var handler in _handlers)
        {
            handler.OnStart(cmdlet);
        }
    }

    public void OnStop(PSCmdlet cmdlet)
    {
        foreach (var handler in _handlers)
        {
            handler.OnStop(cmdlet);
        }
    }

    public void OnEnd(PSCmdlet cmdlet)
    {
        foreach (var handler in _handlers)
        {
            handler.OnEnd(cmdlet);
        }
    }
}
