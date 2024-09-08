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
            var installer = CreateInstaller(context, update);
            Interlocked.Increment(ref _runningCount);
            var job = installer.BeginInstall(
                (job, args) =>
                {
                    var progress = CreateInstallationProgress(installer, args, updateProgressId);
                    _asyncItems.Add(progress);
                    if (args.Progress.CurrentUpdatePercentComplete == 100)
                    {
                        InstallationCompleted(args, installer);
                    }
                },
                (job, args) =>
                {
                    try
                    {
                        var result = installer.EndInstall(job);
                        if (result.ResultCode != WUApiLib.OperationResultCode.orcSucceeded)
                        {
                            var err = ErrorRecordFactory.ErrorRecordForHResult(
                                result.HResult,
                                update,
                                null
                            );
                            _asyncItems.Add(err);
                        }
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
    }

    private void InstallationCompleted(
        IInstallationProgressChangedCallbackArgs args,
        IUpdateInstaller installer
    )
    {
        var singleUpdateResult = args.Progress.GetUpdateResult(args.Progress.CurrentUpdateIndex);
        var singleUpdate = installer.Updates[args.Progress.CurrentUpdateIndex].Map();
        if (singleUpdateResult.ResultCode != WUApiLib.OperationResultCode.orcSucceeded)
        {
            var updateError = ErrorRecordFactory.ErrorRecordForHResult(
                singleUpdateResult.HResult,
                singleUpdate,
                null
            );
            _asyncItems.Add(updateError);
        }
        else
        {
            _asyncItems.Add(singleUpdate);
        }
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
            var downloader = CreateDownloader(context, update);
            Interlocked.Increment(ref _runningCount);
            var job = downloader.BeginDownload(
                (job, args) =>
                {
                    var progress = CreateDownloadProgress(update, args, updateProgressId);
                    _asyncItems.Add(progress);
                },
                (job, args) =>
                {
                    try
                    {
                        var result = downloader.EndDownload(job);
                        for (int i = 0; i < downloader.Updates.Count; i++)
                        {
                            var singleDownloadResult = result.GetUpdateResult(i);
                            if (
                                singleDownloadResult.ResultCode
                                != WUApiLib.OperationResultCode.orcSucceeded
                            )
                            {
                                var failedUpdate = downloader.Updates[i].Map();
                                var updateError = ErrorRecordFactory.ErrorRecordForHResult(
                                    singleDownloadResult.HResult,
                                    failedUpdate,
                                    null
                                );
                                _asyncItems.Add(updateError);
                            }
                        }

                        if (result.ResultCode != WUApiLib.OperationResultCode.orcSucceeded)
                        {
                            var err = ErrorRecordFactory.ErrorRecordForHResult(
                                result.HResult,
                                update,
                                null
                            );
                            _asyncItems.Add(err);
                        }

                        Install(context, update, cancellationToken);
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
    }

    private static void InstallationCompletedInJob(
        AsyncDelegatedJobContext jobContext,
        IInstallationProgressChangedCallbackArgs args,
        IUpdateInstaller installer
    )
    {
        var singleUpdateResult = args.Progress.GetUpdateResult(args.Progress.CurrentUpdateIndex);
        var singleUpdate = installer.Updates[args.Progress.CurrentUpdateIndex].Map();
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
        IUpdateInstaller installer,
        IInstallationProgressChangedCallbackArgs args,
        int updateProgressId
    )
    {
        var update = installer.Updates[args.Progress.CurrentUpdateIndex];
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
                        var progress = CreateInstallationProgress(
                            installer,
                            args,
                            updateProgressId
                        );
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

public interface IWriter
{
    void WriteObject(object obj);
    void WriteError(ErrorRecord error);
    void WriteProgress(ProgressRecord progress);
}

public sealed class JobWriter(AsyncDelegatedJobContext context) : IWriter
{
    public void WriteObject(object obj)
    {
        context.WriteObject(obj);
    }

    public void WriteError(ErrorRecord error)
    {
        context.WriteError(error);
    }

    public void WriteProgress(ProgressRecord progress)
    {
        context.WriteProgress(progress);
    }
}

public sealed class CmdletWriter(BlockingCollection<object?> asyncItems) : IWriter
{
    public void WriteObject(object obj)
    {
        asyncItems.Add(obj);
    }

    public void WriteError(ErrorRecord error)
    {
        asyncItems.Add(error);
    }

    public void WriteProgress(ProgressRecord progress)
    {
        asyncItems.Add(progress);
    }
}
