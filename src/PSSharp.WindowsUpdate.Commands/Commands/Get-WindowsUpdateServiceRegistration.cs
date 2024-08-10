using System.Management.Automation;

namespace PSSharp.WindowsUpdate.Commands;

[Cmdlet(
    VerbsCommon.Get,
    "WindowsUpdateServiceRegistration",
    DefaultParameterSetName = "Default",
    RemotingCapability = RemotingCapability.PowerShell
)]
[OutputType(typeof(WindowsUpdateServiceRegistration))]
public sealed class GetWindowsUpdateServiceRegistrationCommand
    : WindowsUpdateCmdlet<WindowsUpdateServiceContext>
{
    [Parameter(ParameterSetName = "Default", Position = 0, ValueFromPipelineByPropertyName = true)]
    public Guid[] ServiceID { get; set; } = [];

    [Parameter(ParameterSetName = "Pipeline", Mandatory = true, ValueFromPipeline = true)]
    public WindowsUpdateService[] Service { get; set; } = [];

    protected override void ProcessRecord(
        WindowsUpdateServiceContext context,
        CancellationToken cancellationToken
    )
    {
        foreach (var serviceId in ServiceID)
        {
            var registration = context.Manager.QueryServiceRegistration(serviceId.ToString());
            WriteObject(registration);
        }

        foreach (var service in Service)
        {
            var registration = context.Manager.QueryServiceRegistration(
                service.ServiceID.ToString()
            );
            WriteObject(registration);
        }

        if (ServiceID.Length == 0 && Service.Length == 0)
        {
            foreach (var service in context.Manager.Services)
            {
                var registration = context.Manager.QueryServiceRegistration(
                    service.ServiceID.ToString()
                );
                WriteObject(registration);
            }
        }
    }
}
