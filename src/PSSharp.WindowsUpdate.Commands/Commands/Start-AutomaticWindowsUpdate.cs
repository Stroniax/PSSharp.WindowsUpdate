using System.Management.Automation;
using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

[Cmdlet(VerbsLifecycle.Start, "AutomaticWindowsUpdate")]
[Alias("saawu")]
[OutputType(typeof(void))]
public sealed class StartAutomaticWindowsUpdateCommand : WindowsUpdateCmdlet
{
    protected override void ProcessRecord()
    {
        var au = new AutomaticUpdates();

        if (!au.ServiceEnabled)
        {
            ThrowTerminatingError(
                new ErrorRecord(
                    new InvalidOperationException("The Windows Update service is not enabled."),
                    "WindowsUpdateServiceNotEnabled",
                    ErrorCategory.InvalidOperation,
                    au
                )
            );
        }

        au.DetectNow();
    }
}
