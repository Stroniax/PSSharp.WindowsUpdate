using System.Management.Automation;
using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

[Cmdlet(VerbsCommon.Get, "WindowsUpdateHistory")]
[Alias("gwuh")]
[OutputType(typeof(WindowsUpdateHistoryEntry))]
public sealed class GetWindowsUpdateHistoryCommand
    : WindowsUpdateCmdlet<WindowsUpdateHistoryCmdletContext>,
        IWindowsUpdateSessionCmdlet
{
    [Parameter(ParameterSetName = "Default", Position = 0, ValueFromPipelineByPropertyName = true)]
    public Guid[] UpdateId { get; set; } = [];

    [Parameter(ParameterSetName = "Criteria", Position = 0, Mandatory = true)]
    public string? Criteria { get; set; }

    [Parameter]
    public WindowsUpdateSession? Session { get; init; }

    [Parameter(DontShow = true)]
    public int PageSize { get; init; } = 100;

    protected override void ProcessRecord(
        WindowsUpdateHistoryCmdletContext context,
        CancellationToken _
    )
    {
        var sn = Session?.Session ?? context.SessionFactory.CreateSession();

        if (sn is not IUpdateSession3 sn3)
        {
            WriteError(
                new ErrorRecord(
                    new InvalidOperationException(
                        "The current system does not support update history."
                    ),
                    "IUpdateSession3",
                    ErrorCategory.InvalidOperation,
                    sn
                )
            );
            return;
        }

        if (UpdateId.Length > 0)
        {
            Criteria = string.Join(" or ", UpdateId.Select(id => $"UpdateID='{id}'"));
        }

        var offset = 0;
        IUpdateHistoryEntryCollection history;
        do
        {
            history = sn3.QueryHistory(Criteria ?? string.Empty, offset, PageSize);
            foreach (IUpdateHistoryEntry item in history)
            {
                var wrapper = new WindowsUpdateHistoryEntry(item);
                WriteObject(wrapper);
            }
            offset += history.Count;
        } while (history.Count == PageSize);
    }
}
