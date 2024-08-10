using System.Management.Automation;
using System.Runtime.InteropServices;
using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

[Cmdlet(VerbsData.Import, "WindowsUpdate", DefaultParameterSetName = "Default")]
[Alias("ipwu", "dlwu", "Download-WindowsUpdate")]
public sealed class ImportWindowsUpdateCommand : WindowsUpdateCmdlet<WindowsUpdateCmdletContext>
{
    [Parameter(ParameterSetName = "Default", Position = 0)]
    public string[] Title { get; set; } = [];

    [Parameter(ParameterSetName = "Pipeline", Mandatory = true, ValueFromPipeline = true)]
    public WindowsUpdate[] Update { get; set; } = [];

    [Parameter(ParameterSetName = "Path", Mandatory = true)]
    public string Path { get; set; } = string.Empty;

    [Parameter(
        ParameterSetName = "LiteralPath",
        Mandatory = true,
        ValueFromPipelineByPropertyName = true
    )]
    [Alias("PSPath")]
    public string LiteralPath { get; set; } = string.Empty;

    [Parameter(ParameterSetName = "Default")]
    [Parameter(ParameterSetName = "Pipeline")]
    [ValidateSet("Normal", "Low", "High", "ExtraHigh")]
    public string Priority { get; set; } = "Normal";

    [Parameter(ParameterSetName = "Default")]
    [Parameter(ParameterSetName = "Pipeline")]
    public SwitchParameter Force { get; set; }

    protected override void ProcessRecord(
        WindowsUpdateCmdletContext context,
        CancellationToken cancellationToken
    )
    {
        switch (ParameterSetName)
        {
            case "Default":
                NotifyPreferPipeline(context);
                ProcessDefault(context, cancellationToken);
                break;
            case "Pipeline":
                ProcessPipeline(context, cancellationToken);
                break;
            case "Path":
                ProcessPath(context, cancellationToken);
                break;
            case "LiteralPath":
                ProcessLiteralPath(context, cancellationToken);
                break;
        }
    }

    private void NotifyPreferPipeline(WindowsUpdateCmdletContext context)
    {
        if (context.DownloadPreferPipeline.HasNotified())
        {
            return;
        }

        WriteWarning(
            "Prefer piping Windows Updates into 'Import-WindowsUpdate'. "
                + "The Windows Update API requires fully resolved updates. Using the '-Title' "
                + "parameter or similar methods requires re-searching for the update before "
                + "beginning the download which increases the download time."
        );
    }

    private void ProcessDefault(
        WindowsUpdateCmdletContext context,
        CancellationToken cancellationToken
    )
    {
        var downloader = context.Downloader.CreateUpdateDownloader();
        downloader.Priority = GetDownloadPriority(Priority);
        downloader.IsForced = Force;

        try
        {
            var result = downloader.Download(JobProgressCallback, cancellationToken);
            HandleResult(result);
        }
        catch (COMException com)
        {
            var exn = ErrorRecordFactory.CreateBestMatchException(com);
            var err = ErrorRecordFactory.CreateErrorRecord(exn, null, "UpdateDownloadFailure");
            WriteError(err);
        }
    }

    private void ProcessPipeline(
        WindowsUpdateCmdletContext context,
        CancellationToken cancellationToken
    )
    {
        var downloader = context.Downloader.CreateUpdateDownloader();
        downloader.Priority = GetDownloadPriority(Priority);
        downloader.IsForced = Force;
        downloader.Updates = new UpdateCollection();

        foreach (var update in Update)
        {
            downloader.Updates.Add(update.Update);
        }

        try
        {
            var result = downloader.Download(JobProgressCallback, cancellationToken);
            HandleResult(result);
        }
        catch (COMException com)
        {
            var exn = ErrorRecordFactory.CreateBestMatchException(com);
            var err = ErrorRecordFactory.CreateErrorRecord(exn, null, "UpdateDownloadFailure");
            WriteError(err);
        }
    }

    private void ProcessPath(
        WindowsUpdateCmdletContext context,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
    }

    private void ProcessLiteralPath(
        WindowsUpdateCmdletContext context,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
    }

    private void JobProgressCallback(
        IDownloadJob job,
        IDownloadProgressChangedCallbackArgs progress
    )
    {
        var parentRecord = new ProgressRecord(
            0,
            activity: "Downloading Updates",
            statusDescription: "Downloading Updates"
        )
        {
            PercentComplete = progress.Progress.PercentComplete,
            RecordType =
                progress.Progress.PercentComplete == 100
                    ? ProgressRecordType.Completed
                    : ProgressRecordType.Processing,
        };
        WriteProgress(parentRecord);

        var childRecord = new ProgressRecord(
            progress.Progress.CurrentUpdateIndex,
            activity: $"Downloading {job.Updates[progress.Progress.CurrentUpdateIndex]}",
            statusDescription: "Downloading Update"
        )
        {
            ParentActivityId = 0,
            PercentComplete = progress.Progress.CurrentUpdatePercentComplete,
            RecordType =
                progress.Progress.PercentComplete == 100
                    ? ProgressRecordType.Completed
                    : ProgressRecordType.Processing,
        };
        WriteProgress(childRecord);
    }

    private void HandleResult(IDownloadResult result)
    {
        WriteDebug($"Result: {result.ResultCode}.");

        if (result.HResult != 0)
        {
            var com = new COMException(
                $"Unable to download the Windows Updates. The search failed with HResult 0x{result.HResult:X2}",
                result.HResult
            )
            {
#if NET8_0_OR_GREATER
                HResult = result.HResult,
#endif
            };
            var exn = ErrorRecordFactory.CreateBestMatchException(com);
            var error = ErrorRecordFactory.CreateErrorRecord(exn, null, "UpdateDownloadFailure");
            WriteError(error);
        }
    }

    private static DownloadPriority GetDownloadPriority(string priority)
    {
        return priority.ToLower() switch
        {
            "low" => DownloadPriority.dpLow,
            "high" => DownloadPriority.dpHigh,
            "extrahigh" => DownloadPriority.dpExtraHigh,
            _ => DownloadPriority.dpNormal,
        };
    }
}
