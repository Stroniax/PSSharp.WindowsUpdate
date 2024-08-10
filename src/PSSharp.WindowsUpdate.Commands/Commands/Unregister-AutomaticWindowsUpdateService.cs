using System.Management.Automation;
using System.Runtime.InteropServices;

namespace PSSharp.WindowsUpdate.Commands;

[Cmdlet(
    VerbsLifecycle.Unregister,
    "AutomaticWindowsUpdateService",
    SupportsShouldProcess = true,
    RemotingCapability = RemotingCapability.PowerShell,
    DefaultParameterSetName = "Default"
)]
[OutputType(typeof(WindowsUpdateService))]
public sealed class UnregisterAutomaticWindowsUpdateService
    : WindowsUpdateCmdlet<WindowsUpdateServiceContext>
{
    [Parameter(
        ParameterSetName = "Default",
        Position = 0,
        Mandatory = true,
        ValueFromPipelineByPropertyName = true
    )]
    public Guid[] ServiceID { get; set; } = [];

    [Parameter(
        ParameterSetName = "Pipeline",
        Position = 0,
        Mandatory = true,
        ValueFromPipeline = true
    )]
    public WindowsUpdateService[] Service { get; set; } = [];

    [Parameter]
    public SwitchParameter PassThru { get; set; }

    protected override void ProcessRecord(
        WindowsUpdateServiceContext context,
        CancellationToken cancellationToken
    )
    {
        foreach (var serviceId in ServiceID)
        {
            var service = context.Manager.Services.FirstOrDefault(s => s.ServiceID == serviceId);
            if (service is null)
            {
                ErrorNoService(serviceId);
            }
            else
            {
                UnregisterWithAU(context, service);
            }
        }
        foreach (var service in Service)
        {
            UnregisterWithAU(context, service);
        }
    }

    private void ErrorNoService(Guid serviceId)
    {
        var err = ErrorRecordFactory.NotFound("WindowsUpdateService", "ServiceID", serviceId);
        WriteError(err);
    }

    private void UnregisterWithAU(WindowsUpdateServiceContext context, WindowsUpdateService service)
    {
        if (!service.IsRegisteredWithAU)
        {
            var err = ErrorRecordFactory.ServiceNotRegisteredWithAU(service);
            WriteError(err);
            return;
        }

        var should = ShouldProcess(
            $"Unregistering service '{service.Name}' ({service.ServiceID}) from Automatic Updates.",
            $"Unregister service '{service.Name}' ({service.ServiceID}) from Automatic Updates?",
            "Unregister from Automatic Updates"
        );

        if (!should)
        {
            return;
        }

        // Do not allow warning UI: we confirmed via ShouldProcess
        // Must be set every time
        context.Manager.SetAllowWarningUI(false);

        try
        {
            context.Manager.UnregisterServiceWithAU(service.ServiceID.ToString());
        }
        catch (COMException e)
        {
            var er = ErrorRecordFactory.ErrorRecordForHResult(e.HResult, service, e);
            WriteError(er);
            return;
        }

        MaybePassThru(context, service.ServiceID);
    }

    /// <summary>
    /// PassThru the a new instance of the service with updated values
    /// </summary>
    /// <param name="context"></param>
    /// <param name="serviceId"></param>
    private void MaybePassThru(WindowsUpdateServiceContext context, Guid serviceId)
    {
        if (!PassThru)
        {
            return;
        }

        var current = context.Manager.Services.FirstOrDefault(s => s.ServiceID == serviceId);
        WriteObject(current);
    }
}
