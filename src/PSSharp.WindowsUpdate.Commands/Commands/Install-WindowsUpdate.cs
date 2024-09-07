using System.Collections.Concurrent;
using System.Management.Automation;
using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

[Cmdlet(VerbsLifecycle.Install, "WindowsUpdate")]
[OutputType(typeof(WindowsUpdate))]
[Alias("iswu")]
public sealed class InstallWindowsUpdateCommand : WindowsUpdateCmdlet<WindowsUpdateCmdletContext>
{
    private const int MB = 1048576;

    /// <remarks>
    /// Starts at a reasonably high number to not conflict with other progress IDs.
    /// </remarks>
    private static int s_nextProgressId = 2_000;
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

    private void Install(
        WindowsUpdateCmdletContext context,
        WindowsUpdate update,
        CancellationToken cancellationToken
    )
    {
        var updateProgressId = Interlocked.Increment(ref s_nextProgressId);

        if (AsJob)
        {
            var job = AsyncDelegatedJob.Start(
                "WindowsUpdateJob",
                MyInvocation.Line,
                update.Title,
                async (jobContext, token) =>
                {
                    await InstallAsChildJob(context, update, jobContext, updateProgressId);
                },
                CancellationToken.None
            );
            WriteObject(job);
            JobRepository.Add(job);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private static void InstallationCompletedInJob(
        AsyncDelegatedJobContext jobContext,
        IInstallationProgressChangedCallbackArgs args,
        IUpdateInstaller installer
    )
    {
        var singleUpdateResult = args.Progress.GetUpdateResult(args.Progress.CurrentUpdateIndex);
        var singleUpdate = installer.Updates[args.Progress.CurrentUpdateIndex].Map();
        ;
        if (singleUpdateResult.ResultCode != WUApiLib.OperationResultCode.orcSucceeded)
        {
            var updateError = ErrorRecordFactory.ErrorRecordForHResult(
                singleUpdateResult.HResult,
                singleUpdate,
                null
            );
            jobContext.WriteError(updateError);
        }
        else
        {
            jobContext.WriteObject(singleUpdate);
        }
    }

    private IUpdateInstaller CreateInstaller(
        WindowsUpdateCmdletContext context,
        WindowsUpdate update
    )
    {
        var installer = context.Installer.CreateUpdateInstaller();
        installer.Updates = new UpdateCollection { update.Update };
        installer.AllowSourcePrompts = false;
        if (installer is IUpdateInstaller4 i4)
        {
            i4.ForceQuiet = true;
        }
        installer.IsForced = Force;
        return installer;
    }

    private ProgressRecord CreateInstallationProgress(
        WindowsUpdate update,
        IInstallationProgressChangedCallbackArgs args,
        int updateProgressId
    )
    {
        return new ProgressRecord(
            activityId: updateProgressId,
            activity: $"Installing {update.Title}",
            statusDescription: $"Installing... {args.Progress.CurrentUpdatePercentComplete}%"
        )
        {
            ParentActivityId = _cmdletProgressId,
            PercentComplete = args.Progress.CurrentUpdatePercentComplete,
            RecordType = ProgressRecordType.Processing,
        };
    }

    private void Download(
        WindowsUpdateCmdletContext context,
        WindowsUpdate update,
        CancellationToken cancellationToken
    )
    {
        var updateProgressId = Interlocked.Increment(ref s_nextProgressId);

        if (AsJob)
        {
            var job = AsyncDelegatedJob.Start(
                "WindowsUpdateJob",
                MyInvocation.Line,
                update.Title,
                async (jobContext, token) =>
                {
                    await DownloadAsChildJob(context, update, jobContext, updateProgressId);
                    await InstallAsChildJob(context, update, jobContext, updateProgressId);
                },
                CancellationToken.None
            );
            WriteObject(job);
            JobRepository.Add(job);
        }
        else
        {
            throw new NotImplementedException();
        }
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
                    {
                        var progress = CreateDownloadProgress(update, args, updateProgressId);
                        jobContext.WriteProgress(progress);
                    },
                    downloadToken
                );

                for (int i = 0; i < downloader.Updates.Count; i++)
                {
                    var singleDownloadResult = downloadResult.GetUpdateResult(i);
                    if (
                        singleDownloadResult.ResultCode != WUApiLib.OperationResultCode.orcSucceeded
                    )
                    {
                        var failedUpdate = downloader.Updates[i].Map();
                        var updateError = ErrorRecordFactory.ErrorRecordForHResult(
                            singleDownloadResult.HResult,
                            failedUpdate,
                            null
                        );
                        jobContext.WriteError(updateError);
                    }
                }

                if (downloadResult.ResultCode != WUApiLib.OperationResultCode.orcSucceeded)
                {
                    var err = ErrorRecordFactory.ErrorRecordForHResult(
                        downloadResult.HResult,
                        update,
                        null
                    );
                    jobContext.WriteError(err);
                }
            }
        );

        await downloadJob.Task;
    }

    private async Task InstallAsChildJob(
        WindowsUpdateCmdletContext context,
        WindowsUpdate update,
        AsyncDelegatedJobContext jobContext,
        int updateProgressId
    )
    {
        var installJob = jobContext.StartChildAsync(
            "WindowsUpdateInstallJob",
            MyInvocation.Line,
            update.Title,
            async (installJobContext, installToken) =>
            {
                var installer = CreateInstaller(context, update);

                var installProgressId = Interlocked.Increment(ref s_nextProgressId);

                var installResult = await installer.InstallAsync(
                    (job, args) =>
                    {
                        var progress = CreateInstallationProgress(update, args, updateProgressId);
                        jobContext.WriteProgress(progress);

                        if (args.Progress.CurrentUpdatePercentComplete == 100)
                        {
                            InstallationCompletedInJob(jobContext, args, installer);
                        }
                    },
                    installToken
                );

                if (installResult.ResultCode != WUApiLib.OperationResultCode.orcSucceeded)
                {
                    var err = ErrorRecordFactory.ErrorRecordForHResult(
                        installResult.HResult,
                        update,
                        null
                    );
                    jobContext.WriteError(err);
                }
            }
        );

        await installJob.Task;
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

    private ProgressRecord CreateDownloadProgress(
        WindowsUpdate update,
        IDownloadProgressChangedCallbackArgs args,
        int updateProgressId
    )
    {
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

    protected override void EndProcessing(
        WindowsUpdateCmdletContext context,
        CancellationToken cancellationToken
    )
    {
        base.EndProcessing(context, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}
