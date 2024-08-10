using System.Management.Automation;

namespace PSSharp.WindowsUpdate.Commands;

public interface IPSCmdletAccessor
{
    PSCmdlet? Cmdlet { get; }
}
