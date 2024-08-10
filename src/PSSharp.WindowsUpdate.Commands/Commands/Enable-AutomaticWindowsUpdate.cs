using System.Management.Automation;
using System.Runtime.InteropServices;
using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

[Cmdlet(VerbsLifecycle.Enable, "AutomaticWindowsUpdate")]
public sealed class EnableAutomaticWindowsUpdateCommand : Cmdlet
{
    protected override void EndProcessing()
    {
        var auto = new AutomaticUpdates();

        if (auto.ServiceEnabled)
        {
            var error = ErrorRecordFactory.AutomaticUpdatesEnabled();
            ThrowTerminatingError(error);
        }

        try
        {
            auto.EnableService();
        }
        catch (COMException e)
        {
            var error = ErrorRecordFactory.ErrorRecordForHResult(e.HResult, null, e);
            ThrowTerminatingError(error);
        }
    }
}
