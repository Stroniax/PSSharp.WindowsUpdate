using System.Management.Automation;
using PSValueWildcard;

namespace PSSharp.WindowsUpdate.Commands;

[Cmdlet(VerbsCommon.Get, "WindowsUpdateService", DefaultParameterSetName = "Default")]
[OutputType(typeof(WindowsUpdateService))]
public sealed class GetWindowsUpdateServiceCommand
    : WindowsUpdateCmdlet<WindowsUpdateServiceContext>,
        IWindowsUpdateSessionCmdlet
{
    [WindowsUpdateServiceCompletion]
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = "Default")]
    [SupportsWildcards]
    public string[] Name { get; set; } = [];

    [WindowsUpdateServiceCompletion]
    [Parameter(
        Mandatory = true,
        ValueFromPipelineByPropertyName = true,
        ParameterSetName = "ServiceId"
    )]
    public Guid[] ServiceId { get; set; } = [];

    [Parameter]
    public WindowsUpdateSession? Session { get; set; }

    protected override void ProcessRecord(
        WindowsUpdateServiceContext context,
        CancellationToken cancellationToken
    )
    {
        var missing = new HashSet<string>(
            [.. Name, .. ServiceId.Select(id => id.ToString())],
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var service in context.Manager.Services)
        {
            if (
                ParameterSetName == "ServiceId"
                && !ServiceId.Contains(service.ServiceID)
            )
            {
                continue;
            }

            if (
                ParameterSetName == "Default"
                && Name.Length > 0
                && !Name.Any(n =>
                    ValueWildcardPattern.IsMatch(
                        service.Name,
                        n,
                        ValueWildcardOptions.InvariantIgnoreCase
                    )
                )
            )
            {
                continue;
            }

            if (ParameterSetName == "ServiceId")
            {
                missing.Remove(service.ServiceID.ToString());
            }
            else
            {
                missing.Remove(service.Name);
            }
            WriteObject(service);
        }

        foreach (var item in missing)
        {
            var error = ErrorRecordFactory.NotFound(
                "WindowsUpdateService",
                ParameterSetName == "Default" ? "Name" : "ServiceId",
                item
            );
            WriteError(error);
        }
    }
}
