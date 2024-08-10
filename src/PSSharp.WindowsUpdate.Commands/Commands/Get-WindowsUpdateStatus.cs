using System.Management.Automation;
using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

[Cmdlet(VerbsCommon.Get, "WindowsUpdateStatus")]
public sealed class GetWindowsUpdateStatusCommand : Cmdlet
{
    protected override void ProcessRecord()
    {
        var au = new AutomaticUpdates();
        var info = new WindowsUpdateAgentInfo();
        var major = (int)info.GetInfo("ApiMajorVersion");
        var minor = (int)info.GetInfo("ApiMinorVersion");
        var product = (string)info.GetInfo("ProductVersionString");
        var system = new SystemInformation();

        var status = new WindowsUpdateStatus
        {
            AgentVersion = new(major, minor),
            ProductVersion = new(product),
            LastSearchSuccessDate = DateTime
                .SpecifyKind((DateTime)au.Results.LastSearchSuccessDate, DateTimeKind.Utc)
                .ToLocalTime(),
            LastInstallationSuccessDate = DateTime
                .SpecifyKind((DateTime)au.Results.LastInstallationSuccessDate, DateTimeKind.Utc)
                .ToLocalTime(),
            IsAutomaticUpdatesEnabled = au.ServiceEnabled,
            RebootRequired = system.RebootRequired
        };

        WriteObject(status);
    }
}
