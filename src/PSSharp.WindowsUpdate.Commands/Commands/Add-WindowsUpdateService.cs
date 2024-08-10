using System.Management.Automation;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PSSharp.WindowsUpdate.Commands;

[Cmdlet(
    VerbsCommon.Add,
    "WindowsUpdateService",
    ConfirmImpact = ConfirmImpact.Medium,
    SupportsShouldProcess = true,
    DefaultParameterSetName = "Online",
    RemotingCapability = RemotingCapability.PowerShell
)]
[OutputType(typeof(WindowsUpdateServiceRegistration))]
public sealed partial class AddWindowsUpdateServiceCommand
    : WindowsUpdateCmdlet<WindowsUpdateServiceContext>,
        IDynamicParameters
{
    private DynamicParametersImpl? _dynamicParams;

    private sealed class DynamicParametersImpl
    {
        /// <summary>
        /// Skips attempting to register the service immediately. Instead, the service will
        /// only be attempted to be registered later (like <see cref="AllowScheduledRetry"/>)
        /// when Automatic Updates scans.
        /// </summary>
        /// <value></value>
        [Parameter]
        public SwitchParameter SkipImmediateRegistration { get; set; }
    }

    public object? GetDynamicParameters() =>
        AllowScheduledRetry ? _dynamicParams ??= new DynamicParametersImpl() : null;

    private const int AllowPendingRegistrationFlag = 0x1;
    private const int AllowOnlineRegistrationFlag = 0x2;
    private const int RegisterServiceWithAUFlag = 0x4;

    [Parameter(ParameterSetName = "Online", Position = 0, Mandatory = true)]
    [Parameter(ParameterSetName = "Local", Position = 0, Mandatory = true)]
    public Guid ServiceID { get; set; }

    [Parameter(ParameterSetName = "Local", Position = 1, Mandatory = true)]
    [ValidateNotNull]
    public string AuthorizationCabPath { get; set; } = string.Empty;

    /// <summary>
    /// Flips on the 'AllowPendingRegistration' flag which allows the service to be registered
    /// during the next Automatic Updates scan, instead of being registered immediately.
    /// </summary>
    /// <value></value>
    [Parameter(ParameterSetName = "Online")]
    [Alias("AllowPendingRegistration")]
    public SwitchParameter AllowScheduledRetry { get; set; }

    /// <summary>
    /// When <see cref="AllowScheduledRetry"/> is <see langword="true"/>, it still will register with AU
    /// once the service has been registered later. Otherwise, it will register with AU immediately.
    /// </summary>
    /// <value></value>
    [Parameter(ParameterSetName = "Online")]
    [Parameter(ParameterSetName = "Local")]
    public SwitchParameter RegisterServiceWithAU { get; set; }

    protected override void ProcessRecord(
        WindowsUpdateServiceContext context,
        CancellationToken cancellationToken
    )
    {
        var manager = context.Manager;

        var flags = GetFlags();

        var paths =
            AuthorizationCabPath.Length > 0
                ? SessionState.Path.GetResolvedProviderPathFromProviderPath(
                    AuthorizationCabPath,
                    "FileSystem"
                )
                : null;

        if (paths is { Count: > 1 })
        {
            WriteError(
                new ErrorRecord(
                    new AmbiguousMatchException(
                        "The AuthorizationCabPath parameter must resolve to a single path."
                    ),
                    "AmbiguousPath",
                    ErrorCategory.InvalidArgument,
                    AuthorizationCabPath
                )
            );
        }

        if (!ShouldProcess(ServiceID.ToString()))
        {
            return;
        }

        try
        {
            var registration = manager.AddService(
                ServiceID.ToString(),
                flags,
                paths?.FirstOrDefault()
            );

            WriteObject(registration);
        }
        catch (COMException e)
        {
            var err = ErrorRecordFactory.ErrorRecordForHResult(e.HResult, ServiceID, e);
            WriteError(err);

            if (AllowScheduledRetry)
            {
                WriteWarning(
                    $"The service {ServiceID} failed to be added. The service manager will attempt to "
                        + "register the service during the next Automatic Updates scan."
                );
            }

            return;
        }
    }

    private int GetFlags()
    {
        var flags = 0;
        if (ParameterSetName == "Online")
        {
            if (_dynamicParams?.SkipImmediateRegistration is not { IsPresent: true })
            {
                flags |= AllowOnlineRegistrationFlag;
            }
        }
        if (AllowScheduledRetry)
        {
            flags |= AllowPendingRegistrationFlag;
        }
        if (RegisterServiceWithAU)
        {
            flags |= RegisterServiceWithAUFlag;
        }

        return flags;
    }
}
