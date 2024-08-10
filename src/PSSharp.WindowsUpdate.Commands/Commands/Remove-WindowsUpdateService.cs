using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Runtime.InteropServices;

namespace PSSharp.WindowsUpdate.Commands;

[Cmdlet(
    VerbsCommon.Remove,
    "WindowsUpdateService",
    SupportsShouldProcess = true,
    DefaultParameterSetName = "Default"
)]
[OutputType(typeof(void))]
public sealed class RemoveWindowsUpdateServiceCommand
    : WindowsUpdateCmdlet<WindowsUpdateServiceContext>
{
    [Parameter(
        ParameterSetName = "Default",
        Position = 0,
        Mandatory = true,
        ValueFromPipelineByPropertyName = true
    )]
    [WindowsUpdateServiceCompletion]
    public Guid[] ServiceID { get; set; } = [];

    [Parameter(
        ParameterSetName = "Pipeline",
        Position = 0,
        Mandatory = true,
        ValueFromPipeline = true
    )]
    public WindowsUpdateService[] Service { get; set; } = [];

    [Parameter]
    public SwitchParameter Force { get; set; }

    protected override void BeginProcessing(
        WindowsUpdateServiceContext context,
        CancellationToken cancellationToken
    )
    {
        if (!context.Administrator.IsAdministrator)
        {
            ThrowTerminatingError(
                ErrorRecordFactory.AdministratorRequired("remove Windows Update Service")
            );
        }
    }

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
                RemoveService(context, service);
            }
        }

        foreach (var service in Service)
        {
            // Could be piping in a service that is already removed
            if (!context.Manager.Services.Any(s => s.ServiceID == service.ServiceID))
            {
                ErrorNoService(service.ServiceID);
            }
            else
            {
                RemoveService(context, service);
            }
        }
    }

    private void ErrorNoService(Guid serviceId)
    {
        var error = ErrorRecordFactory.NotFound("WindowsUpdateService", "ServiceId", serviceId);
        WriteError(error);
    }

    private void RemoveService(WindowsUpdateServiceContext context, WindowsUpdateService service)
    {
        var should = ShouldProcess(
            $"Removing service '{service.Name}' ({service.ServiceID}).",
            $"Remove service '{service.Name}' ({service.ServiceID})?",
            $"Remove Windows Update Service",
            out var reason
        );

        // For the default update services, provide an additional warning
        if (BuiltInServiceIds.Contains(service.ServiceID) && !Force)
        {
            if (reason == ShouldProcessReason.WhatIf)
            {
                WriteWarning(
                    $"The service '{service.Name}' ({service.ServiceID}) is one of the built-in Windows "
                        + $"Update Services. Removing it may change expected system behavior. To remove "
                        + $"the service non-interactively will require the -Force parameter."
                );
            }
            else if (should)
            {
                should = ShouldContinue(
                    $"The service '{service.Name}' ({service.ServiceID}) is one of the built-in Windows Update Services. Removing it may change expected system behavior. Remove the service?",
                    "Removing Built-In Windows Update Service"
                );
            }
        }

        if (!should)
        {
            return;
        }

        try
        {
            context.Manager.RemoveService(service.ServiceID.ToString());
        }
        catch (COMException e)
        {
            var err = ErrorRecordFactory.ErrorRecordForHResult(e.HResult, service.ServiceID, e);
            WriteError(err);
            return;
        }
    }

    /// <summary>
    /// Identifies Windows Update services that are built-in to Windows. An additional confirmation
    /// is required to remove these services.
    /// <see href="https://learn.microsoft.com/en-us/windows/deployment/update/how-windows-update-works#identifies-service-ids"/>
    /// </summary>
    private readonly Guid[] BuiltInServiceIds =
    [
        // Windows Update
        Guid.Parse("9482F4B4-E343-43B6-B170-9A65BC822C77"),
        // Microsoft Update
        Guid.Parse("7971F918-A847-4430-9279-4A52D1EFE18D"),
        // Windows Store
        Guid.Parse("E23DD3E6-778E-49D4-B537-38FCDE4887D8"),
        // Windows Store (DCat Prod)
        Guid.Parse("855e8a7c-ecb4-4ca3-b045-1dfa50104289"),
        // OS Flighting
        Guid.Parse("8B24B027-1DEE-BABB-9A95-3517DFB9C552"),
        // WSUS or Configuration Manager
        Guid.Parse("3DA21691-E39D-4DA6-8A4B-B43877BCB1B7"),
    ];
}
