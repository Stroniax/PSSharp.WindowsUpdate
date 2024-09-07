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
            WriteDebug($"Starting job for {update.Title}.");

            var job = AsyncDelegatedJob.Start(
                "WindowsUpdateJob",
                MyInvocation.Line,
                update.Title,
                async (jobContext, token) =>
                {
                    jobContext.Job.Debug.Add(
                        new DebugRecord($"Job to install {update.Title} is running.")
                    );

                    jobContext.Job.Debug.Add(
                        new DebugRecord($"Download job for {update.Title} skipped.")
                    );

                    jobContext.Job.Debug.Add(
                        new DebugRecord($"Starting install job for {update.Title}.")
                    );

                    var installJob = jobContext.StartChildAsync(
                        "WindowsUpdateInstallJob",
                        MyInvocation.Line,
                        update.Title,
                        async (installJobContext, installToken) =>
                        {
                            jobContext.Job.Debug.Add(
                                new DebugRecord($"Child install job for {update.Title} is running.")
                            );

                            var installer = new UpdateInstaller
                            {
                                Updates = new UpdateCollection { update.Update },
                                AllowSourcePrompts = false,
                                ForceQuiet = true,
                            };

                            var installProgressId = Interlocked.Increment(ref s_nextProgressId);

                            var installResult = await installer.InstallAsync(
                                (job, args) =>
                                {
                                    var progress = new ProgressRecord(
                                        activityId: updateProgressId,
                                        activity: $"Installing {update.Title}",
                                        statusDescription: $"Installing... {args.Progress.CurrentUpdatePercentComplete}%"
                                    )
                                    {
                                        ParentActivityId = _cmdletProgressId,
                                        PercentComplete =
                                            args.Progress.CurrentUpdatePercentComplete,
                                        RecordType = ProgressRecordType.Processing,
                                    };
                                    jobContext.WriteProgress(progress);

                                    if (args.Progress.CurrentUpdatePercentComplete == 100)
                                    {
                                        var singleUpdateResult = args.Progress.GetUpdateResult(
                                            args.Progress.CurrentUpdateIndex
                                        );
                                        var singleUpdate = installer
                                            .Updates[args.Progress.CurrentUpdateIndex]
                                            .Map();
                                        ;
                                        if (
                                            singleUpdateResult.ResultCode
                                            != WUApiLib.OperationResultCode.orcSucceeded
                                        )
                                        {
                                            var updateError =
                                                ErrorRecordFactory.ErrorRecordForHResult(
                                                    singleUpdateResult.HResult,
                                                    singleUpdate,
                                                    null
                                                );
                                            jobContext.WriteError(updateError);
                                        }
                                        else
                                        {
                                            jobContext.WriteVerbose(
                                                $"Successfully installed {singleUpdate.Title}."
                                            );
                                            jobContext.WriteObject(singleUpdate);
                                        }
                                    }
                                },
                                installToken
                            );

                            if (
                                installResult.ResultCode
                                != WUApiLib.OperationResultCode.orcSucceeded
                            )
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

    private void Download(
        WindowsUpdateCmdletContext context,
        WindowsUpdate update,
        CancellationToken cancellationToken
    )
    {
        var updateProgressId = Interlocked.Increment(ref s_nextProgressId);

        if (AsJob)
        {
            WriteDebug($"Starting job for {update.Title}.");

            var job = AsyncDelegatedJob.Start(
                "WindowsUpdateJob",
                MyInvocation.Line,
                update.Title,
                async (jobContext, token) =>
                {
                    jobContext.Job.Debug.Add(
                        new DebugRecord($"Job to install {update.Title} is running.")
                    );

                    jobContext.Job.Debug.Add(
                        new DebugRecord($"Starting download job for {update.Title}.")
                    );

                    var downloadJob = jobContext.StartChildAsync(
                        "WindowsUpdateDownloadJob",
                        MyInvocation.Line,
                        update.Title,
                        async (downloadJobContext, downloadToken) =>
                        {
                            jobContext.Job.Debug.Add(
                                new DebugRecord(
                                    $"Child download job for {update.Title} is running."
                                )
                            );

                            var downloader = new UpdateDownloader
                            {
                                Updates = new UpdateCollection { update.Update }
                            };

                            var downloadResult = await downloader.DownloadAsync(
                                (job, args) =>
                                {
                                    var progressDescription =
                                        args.Progress.CurrentUpdateDownloadPhase switch
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
                                    jobContext.WriteProgress(progress);
                                },
                                downloadToken
                            );

                            for (int i = 0; i < downloader.Updates.Count; i++)
                            {
                                var singleDownloadResult = downloadResult.GetUpdateResult(i);
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
                                    jobContext.WriteError(updateError);
                                }
                            }

                            if (
                                downloadResult.ResultCode
                                != WUApiLib.OperationResultCode.orcSucceeded
                            )
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

                    jobContext.Job.Debug.Add(
                        new DebugRecord($"Waiting for download job for {update.Title} to complete.")
                    );

                    await downloadJob.Task;

                    jobContext.Job.Debug.Add(
                        new DebugRecord($"Download job for {update.Title} completed.")
                    );

                    jobContext.Job.Debug.Add(
                        new DebugRecord($"Starting install job for {update.Title}.")
                    );

                    var installJob = jobContext.StartChildAsync(
                        "WindowsUpdateInstallJob",
                        MyInvocation.Line,
                        update.Title,
                        async (installJobContext, installToken) =>
                        {
                            jobContext.Job.Debug.Add(
                                new DebugRecord($"Child install job for {update.Title} is running.")
                            );

                            var installer = new UpdateInstaller
                            {
                                Updates = new UpdateCollection { update.Update },
                                AllowSourcePrompts = false,
                                ForceQuiet = true,
                            };

                            var installProgressId = Interlocked.Increment(ref s_nextProgressId);

                            var installResult = await installer.InstallAsync(
                                (job, args) =>
                                {
                                    var progress = new ProgressRecord(
                                        activityId: updateProgressId,
                                        activity: $"Installing {update.Title}",
                                        statusDescription: $"Installing... {args.Progress.CurrentUpdatePercentComplete}%"
                                    )
                                    {
                                        ParentActivityId = _cmdletProgressId,
                                        PercentComplete =
                                            args.Progress.CurrentUpdatePercentComplete,
                                        RecordType = ProgressRecordType.Processing,
                                    };
                                    jobContext.WriteProgress(progress);

                                    if (args.Progress.CurrentUpdatePercentComplete == 100)
                                    {
                                        var singleUpdateResult = args.Progress.GetUpdateResult(
                                            args.Progress.CurrentUpdateIndex
                                        );
                                        var singleUpdate = installer
                                            .Updates[args.Progress.CurrentUpdateIndex]
                                            .Map();
                                        ;
                                        if (
                                            singleUpdateResult.ResultCode
                                            != WUApiLib.OperationResultCode.orcSucceeded
                                        )
                                        {
                                            var updateError =
                                                ErrorRecordFactory.ErrorRecordForHResult(
                                                    singleUpdateResult.HResult,
                                                    singleUpdate,
                                                    null
                                                );
                                            jobContext.WriteError(updateError);
                                        }
                                        else
                                        {
                                            jobContext.WriteVerbose(
                                                $"Successfully installed {singleUpdate.Title}."
                                            );
                                            jobContext.WriteObject(singleUpdate);
                                        }
                                    }
                                },
                                installToken
                            );

                            if (
                                installResult.ResultCode
                                != WUApiLib.OperationResultCode.orcSucceeded
                            )
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
