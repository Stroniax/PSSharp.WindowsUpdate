using System.Management.Automation;

namespace PSSharp.WindowsUpdate.Commands;

public sealed class PSCmdletSessionStateAccessor(IPSCmdletAccessor cmdlet) : ISessionStateAccessor
{
    private readonly IPSCmdletAccessor _cmdlet = cmdlet;

    public SessionState? SessionState => _cmdlet.Cmdlet?.MyInvocation.MyCommand.Module.SessionState;
}
