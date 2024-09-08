using System.Management.Automation;
using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public sealed partial class InstallWindowsUpdateCommand
{
    private void Download(
        WindowsUpdateCmdletContext context,
        WindowsUpdate update,
        CancellationToken cancellationToken
    )
    {
        var updateProgressId = Interlocked.Increment(ref s_nextProgressId);

        if (AsJob)
        {
            DownloadAsJob(context, update, updateProgressId);
        }
        else
        {
            StartDownload(context, update, updateProgressId, cancellationToken);
        }
    }

    private void StartDownload(
        WindowsUpdateCmdletContext context,
        WindowsUpdate update,
        int updateProgressId,
        CancellationToken cancellationToken
    )
    {
        var downloader = CreateDownloader(context, update);
        Interlocked.Increment(ref _runningCount);
        var job = downloader.BeginDownload(
            (job, args) =>
            {
                OnDownloadProgress(
                    job,
                    args,
                    updateProgressId,
                    _asyncItems.Add,
                    _asyncItems.Add,
                    (update) =>
                    {
                        Install(
                            context,
                            job.Updates[args.Progress.CurrentUpdateIndex].Map(),
                            cancellationToken
                        );
                    }
                );
            },
            (job, args) =>
            {
                try
                {
                    var result = downloader.EndDownload(job);
                    OnDownloadJobCompleted(result, update, _asyncItems.Add);
                }
                finally
                {
                    DecrementRunningCount();
                }
            },
            null
        );

        cancellationToken.Register(job.RequestAbort);
    }

    private void DownloadAsJob(
        WindowsUpdateCmdletContext context,
        WindowsUpdate update,
        int updateProgressId
    )
    {
        var job = AsyncDelegatedJob.Start(
            "WindowsUpdateJob",
            MyInvocation.Line,
            update.Title,
            async (jobContext, token) =>
            {
                await DownloadAsChildJob(context, update, jobContext, updateProgressId);
                await ((AsyncDelegatedJob)jobContext.Job.ChildJobs[1]).Task;
                // await InstallAsChildJob(context, update, jobContext, updateProgressId);
            },
            CancellationToken.None
        );
        WriteObject(job);
        JobRepository.Add(job);
    }

    private async Task DownloadAsChildJob(
        WindowsUpdateCmdletContext context,
        WindowsUpdate update,
        AsyncDelegatedJobContext jobContext,
        int updateProgressId
    )
    {
        var downloadJob = jobContext.StartChildAsync(
            "WindowsUpdateDownloadJob",
            MyInvocation.Line,
            update.Title,
            async (downloadJobContext, downloadToken) =>
            {
                var downloader = CreateDownloader(context, update);

                var downloadResult = await downloader.DownloadAsync(
                    (job, args) =>
                        OnDownloadProgress(
                            job,
                            args,
                            updateProgressId,
                            downloadJobContext.WriteProgress,
                            downloadJobContext.WriteError,
                            update =>
                            {
                                _ = InstallAsChildJob(
                                    context,
                                    update.Map(),
                                    jobContext,
                                    updateProgressId
                                );
                            }
                        ),
                    downloadToken
                );

                OnDownloadJobCompleted(downloadResult, update, jobContext.WriteError);
            }
        );

        await downloadJob.Task;
    }

    private IUpdateDownloader CreateDownloader(
        WindowsUpdateCmdletContext context,
        WindowsUpdate update
    )
    {
        var downloader = context.Downloader.CreateUpdateDownloader();
        downloader.Updates = new UpdateCollection { update.Update };
        downloader.IsForced = Force;
        return downloader;
    }

    private void OnDownloadProgress(
        IDownloadJob job,
        IDownloadProgressChangedCallbackArgs args,
        int updateProgressId,
        Action<ProgressRecord> writeProgress,
        Action<ErrorRecord> writeError,
        Action<IUpdate> downloadCompleted
    )
    {
        var progress = CreateDownloadProgress(job, args, updateProgressId);
        writeProgress(progress);

        if (args.Progress.CurrentUpdatePercentComplete == 100)
        {
            OnDownloadCompleted(job, args, downloadCompleted, writeError);
        }
    }

    private static void OnDownloadCompleted(
        IDownloadJob job,
        IDownloadProgressChangedCallbackArgs args,
        Action<IUpdate> downloaded,
        Action<ErrorRecord> writeError
    )
    {
        var result = args.Progress.GetUpdateResult(args.Progress.CurrentUpdateIndex);
        var update = job.Updates[args.Progress.CurrentUpdateIndex];

        if (result.ResultCode == WUApiLib.OperationResultCode.orcSucceeded)
        {
            downloaded(update);
        }
        else
        {
            var updateError = ErrorRecordFactory.ErrorRecordForHResult(
                result.HResult,
                update.Map(),
                null
            );
            writeError(updateError);
        }
    }

    private static void OnDownloadJobCompleted(
        IDownloadResult result,
        WindowsUpdate update,
        Action<ErrorRecord> writeError
    )
    {
        if (result.ResultCode != WUApiLib.OperationResultCode.orcSucceeded)
        {
            var err = ErrorRecordFactory.ErrorRecordForHResult(result.HResult, update, null);
            writeError(err);
        }
    }

    private ProgressRecord CreateDownloadProgress(
        IDownloadJob downloader,
        IDownloadProgressChangedCallbackArgs args,
        int updateProgressId
    )
    {
        var update = downloader.Updates[args.Progress.CurrentUpdateIndex];
        var progressDescription = args.Progress.CurrentUpdateDownloadPhase switch
        {
            DownloadPhase.dphDownloading
                => $"{args.Progress.CurrentUpdateBytesDownloaded / MB}mb / {args.Progress.CurrentUpdateBytesToDownload / MB}mb",
            DownloadPhase.dphVerifying => "Verifying",
            _ => args.Progress.CurrentUpdateDownloadPhase.ToString()
        };
        var progress = new ProgressRecord(
            activityId: updateProgressId,
            activity: $"Downloading {update.Title}",
            statusDescription: progressDescription
        )
        {
            ParentActivityId = _cmdletProgressId,
            PercentComplete = args.Progress.PercentComplete,
            RecordType = ProgressRecordType.Processing,
        };
        return progress;
    }

    private void NotDownloadedError(WindowsUpdate update)
    {
        throw new NotImplementedException();
    }
}
