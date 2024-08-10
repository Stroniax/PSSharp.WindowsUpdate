using System.Management.Automation;
using System.Runtime.InteropServices;

namespace PSSharp.WindowsUpdate.Commands;

[Cmdlet(
    VerbsLifecycle.Register,
    "AutomaticWindowsUpdateService",
    SupportsShouldProcess = true,
    RemotingCapability = RemotingCapability.PowerShell,
    DefaultParameterSetName = "Default"
)]
[OutputType(typeof(WindowsUpdateService))]
public sealed class RegisterAutomaticWindowsUpdateServiceCommand
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
                RegisterWithAU(context, service);
            }
        }
        foreach (var service in Service)
        {
            RegisterWithAU(context, service);
        }
    }

    private void ErrorNoService(Guid serviceId)
    {
        var error = ErrorRecordFactory.NotFound("WindowsUpdateService", "ServiceID", serviceId);
        WriteError(error);
    }

    private void RegisterWithAU(WindowsUpdateServiceContext context, WindowsUpdateService service)
    {
        if (service.IsRegisteredWithAU)
        {
            var err = ErrorRecordFactory.ServiceRegisteredWithAU(service);
            WriteError(err);
            return;
        }
        if (!service.CanRegisterWithAU)
        {
            var err = ErrorRecordFactory.ServiceCannotRegisterWithAU(service);
            WriteError(err);
            return;
        }

        var should = ShouldProcess(
            $"Registering service '{service.Name}' ({service.ServiceID}) with Automatic Updates.",
            $"Register service '{service.Name}' ({service.ServiceID}) with Automatic Updates?",
            "Register Service with AU"
        );

        if (!should)
        {
            return;
        }

        try
        {
            context.Manager.RegisterServiceWithAU(service.ServiceID.ToString());
        }
        catch (COMException e)
        {
            var err = ErrorRecordFactory.ErrorRecordForHResult(e.HResult, service, e);
            WriteError(err);
            return;
        }

        MaybePassThru(context, service.ServiceID);
    }

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
