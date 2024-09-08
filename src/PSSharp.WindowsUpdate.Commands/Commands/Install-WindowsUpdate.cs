using System.Collections.Concurrent;
using System.Management.Automation;
using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

[Cmdlet(VerbsLifecycle.Install, "WindowsUpdate")]
[OutputType(typeof(WindowsUpdate))]
[Alias("iswu")]
public sealed partial class InstallWindowsUpdateCommand
    : WindowsUpdateCmdlet<WindowsUpdateCmdletContext>
{
    private const int MB = 1048576;

    /// <remarks>
    /// Starts at a reasonably high number to not conflict with other progress IDs.
    /// </remarks>
    private static int s_nextProgressId = 2_000;
    private int _runningCount = 1;
    private readonly BlockingCollection<object?> _asyncItems = [];
    private int _cmdletProgressId = Interlocked.Increment(ref s_nextProgressId);

    [Parameter(
        ParameterSetName = "Pipeline",
        Position = 0,
        Mandatory = true,
        ValueFromPipeline = true
    )]
    [Alias("WindowsUpdate")]
    public WindowsUpdate[] Update { get; set; } = [];

    /// <summary>
    /// By default, installing an update will also download it if it is not already downloaded. This is
    /// designed as a convenience and optimization in most places, while remaining idiomatic in usage.
    /// However, this switch parameter disables that functionality. When present, only updates that
    /// are already downloaded will be installed.
    /// </summary>
    [Parameter]
    public SwitchParameter DoNotDownload { get; set; }

    [Parameter]
    public SwitchParameter Force { get; set; }

    [Parameter]
    public SwitchParameter AsJob { get; set; }

    protected override void ProcessRecord(
        WindowsUpdateCmdletContext context,
        CancellationToken cancellationToken
    )
    {
        foreach (var update in Update)
        {
            ProcessUpdate(context, update, cancellationToken);
        }

        base.ProcessRecord(context, cancellationToken);

        DrainPending();
    }

    protected override void EndProcessing(
        WindowsUpdateCmdletContext context,
        CancellationToken cancellationToken
    )
    {
        base.EndProcessing(context, cancellationToken);

        DecrementRunningCount();
        DrainUntilCompleted(cancellationToken);
    }

    private void ProcessUpdate(
        WindowsUpdateCmdletContext context,
        WindowsUpdate update,
        CancellationToken cancellationToken
    )
    {
        if (!update.IsDownloaded)
        {
            if (DoNotDownload)
            {
                NotDownloadedError(update);
                return;
            }
            Download(context, update, cancellationToken);
            return;
        }
        Install(context, update, cancellationToken);
    }

    private void DrainUntilCompleted(CancellationToken cancellationToken)
    {
        foreach (var obj in _asyncItems.GetConsumingEnumerable(cancellationToken))
        {
            if (obj is ErrorRecord err)
            {
                WriteError(err);
            }
            else if (obj is ProgressRecord prog)
            {
                WriteProgress(prog);
            }
            else
            {
                WriteObject(obj);
            }
        }
    }

    private void DrainPending()
    {
        while (_asyncItems.TryTake(out var obj))
        {
            if (obj is ErrorRecord err)
            {
                WriteError(err);
            }
            else if (obj is ProgressRecord prog)
            {
                WriteProgress(prog);
            }
            else
            {
                WriteObject(obj);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        _asyncItems.Dispose();
        base.Dispose(disposing);
    }

    private void DecrementRunningCount()
    {
        if (Interlocked.Decrement(ref _runningCount) == 0)
        {
            _asyncItems.CompleteAdding();
        }
    }
}
