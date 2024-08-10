using System.Management.Automation;

namespace PSSharp.WindowsUpdate.Commands;

public sealed class PSCmdletAccessor : IPSCmdletAccessor
{
    public PSCmdlet? Cmdlet { get; set; }
}
