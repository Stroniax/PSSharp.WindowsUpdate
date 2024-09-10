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
    private readonly BlockingCollection<object?> _asyncItems = [];
    private int _cmdletProgressId = Interlocked.Increment(ref s_nextProgressId);

    private readonly List<IUpdate> _updatesToProcess = [];

    [Parameter(
        ParameterSetName = "Pipeline",
        Position = 0,
        Mandatory = true,
        ValueFromPipeline = true
    )]
    [Alias("WindowsUpdate", "wu")]
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
        base.ProcessRecord(context, cancellationToken);

        foreach (var update in Update)
        {
            if (DoNotDownload && !update.IsDownloaded)
            {
                NotDownloadedError(update);
                continue;
            }

            if (!ShouldProcess(update.Title, "Download & Install"))
            {
                continue;
            }

            _updatesToProcess.Add(update.Update);
        }
    }

    private void NotDownloadedError(WindowsUpdate update)
    {
        var exn = new InvalidOperationException($"The update is not downloaded.");

        var err = new ErrorRecord(
            exn,
            "UpdateNotDownloaded",
            ErrorCategory.InvalidOperation,
            update
        )
        {
            ErrorDetails = new ErrorDetails($"The update '{update.Title}' is not downloaded.")
            {
                RecommendedAction = "Download the update before attempting to install it."
            }
        };

        WriteError(err);
    }

    protected override void EndProcessing(
        WindowsUpdateCmdletContext context,
        CancellationToken cancellationToken
    )
    {
        base.EndProcessing(context, cancellationToken);

        var downloader = context.Downloader.CreateUpdateDownloader();
        downloader.Updates = new UpdateCollection();
        var installer = context.Installer.CreateUpdateInstaller();
        foreach (var update in _updatesToProcess)
        {
            if (!update.IsDownloaded)
            {
                downloader.Updates.Add(update);
            }
            else
            {
                installer.Updates.Add(update);
            }
        }
        if (AsJob)
        {
            var job = AsyncDelegatedJob.Start(
                "WindowsUpdateJob",
                MyInvocation.Line,
                "Installing Windows Updates",
                async (jobContext, token) =>
                {
                    if (downloader.Updates.Count > 0)
                    {
                        var downloadResult = await downloader.DownloadAsync(
                            (job, args) =>
                            {
                                OnDownloadProgress(
                                    job,
                                    args,
                                    _cmdletProgressId,
                                    jobContext.WriteProgress,
                                    jobContext.WriteError,
                                    update => installer.Updates.Add(update)
                                );
                            },
                            cancellationToken
                        );

                        OnJobCompleted(
                            downloadResult.ResultCode,
                            downloadResult.HResult,
                            jobContext.WriteError
                        );
                        if (
                            downloadResult.ResultCode
                            is WUApiLib.OperationResultCode.orcFailed
                                or WUApiLib.OperationResultCode.orcAborted
                        )
                        {
                            return;
                        }
                    }

                    if (installer.Updates.Count > 0)
                    {
                        var installResult = await installer.InstallAsync(
                            (job, args) =>
                            {
                                OnInstallProgress(
                                    job,
                                    args,
                                    _cmdletProgressId,
                                    jobContext.WriteProgress,
                                    jobContext.WriteError,
                                    jobContext.WriteObject
                                );
                            },
                            cancellationToken
                        );

                        OnJobCompleted(
                            installResult.ResultCode,
                            installResult.HResult,
                            jobContext.WriteError
                        );
                    }
                },
                CancellationToken.None
            );
        }
        else
        {
            IDownloadJob? downloadJob = null;
            IInstallationJob? installJob = null;
            if (downloader.Updates.Count > 0)
            {
                downloadJob = downloader.BeginDownload(
                    // DOWNLOAD PROGRESS
                    (job, args) =>
                    {
                        OnDownloadProgress(
                            job,
                            args,
                            _cmdletProgressId,
                            _asyncItems.Add,
                            _asyncItems.Add,
                            update => installer.Updates.Add(update)
                        );
                    },
                    // DOWNLOAD COMPLETED
                    (job, args) =>
                    {
                        var result = downloader.EndDownload(job);
                        OnJobCompleted(result.ResultCode, result.HResult, _asyncItems.Add);
                        if (
                            result.ResultCode
                                is WUApiLib.OperationResultCode.orcFailed
                                    or WUApiLib.OperationResultCode.orcAborted
                            || installer.Updates.Count == 0
                        )
                        {
                            _asyncItems.CompleteAdding();
                            return;
                        }

                        installJob = installer.BeginInstall(
                            // INSTALL PROGRESS
                            (job, args) =>
                            {
                                OnInstallProgress(
                                    job,
                                    args,
                                    _cmdletProgressId,
                                    _asyncItems.Add,
                                    _asyncItems.Add,
                                    _asyncItems.Add
                                );
                            },
                            // INSTALL COMPLETED
                            (job, args) =>
                            {
                                try
                                {
                                    var result = installer.EndInstall(job);
                                    OnJobCompleted(
                                        result.ResultCode,
                                        result.HResult,
                                        _asyncItems.Add
                                    );
                                }
                                catch (Exception exn)
                                {
                                    _asyncItems.Add(
                                        ErrorRecordFactory.CreateErrorRecord(
                                            exn,
                                            null,
                                            "EndInstallJobError"
                                        )
                                    );
                                }
                                finally
                                {
                                    _asyncItems.CompleteAdding();
                                }
                            },
                            null
                        );

                        cancellationToken.Register(() => installJob.RequestAbort());
                    },
                    null
                );
            }
            else if (installer.Updates.Count > 0)
            {
                installJob = installer.BeginInstall(
                    // INSTALL PROGRESS
                    (job, args) =>
                    {
                        OnInstallProgress(
                            job,
                            args,
                            _cmdletProgressId,
                            _asyncItems.Add,
                            _asyncItems.Add,
                            _asyncItems.Add
                        );
                    },
                    // INSTALL COMPLETED
                    (job, args) =>
                    {
                        try
                        {
                            var result = installer.EndInstall(job);
                            OnJobCompleted(result.ResultCode, result.HResult, _asyncItems.Add);
                        }
                        catch (Exception exn)
                        {
                            _asyncItems.Add(
                                ErrorRecordFactory.CreateErrorRecord(
                                    exn,
                                    null,
                                    "EndInstallJobError"
                                )
                            );
                        }
                        finally
                        {
                            _asyncItems.CompleteAdding();
                        }
                    },
                    null
                );
            }

            using var creg = cancellationToken.Register(() => downloadJob?.RequestAbort());
            using var creg2 = cancellationToken.Register(() => installJob?.RequestAbort());

            DrainUntilCompleted(cancellationToken);

            downloadJob?.CleanUp();
            installJob?.CleanUp();
        }
    }

    private static void OnDownloadProgress(
        IDownloadJob job,
        IDownloadProgressChangedCallbackArgs args,
        int cmdletProgressId,
        Action<ProgressRecord> writeProgress,
        Action<ErrorRecord> writeError,
        Action<IUpdate> downloadCompleted
    )
    {
        var update = job.Updates[args.Progress.CurrentUpdateIndex];

        var totalProgress = new ProgressRecord(
            cmdletProgressId,
            "Installing Windows Updates",
            "Downloading"
        )
        {
            PercentComplete = args.Progress.PercentComplete / 2,
        };

        var updateProgress = new ProgressRecord(
            cmdletProgressId + args.Progress.CurrentUpdateIndex,
            $"Downloading {update.Title}",
            args.Progress.CurrentUpdateDownloadPhase switch
            {
                DownloadPhase.dphInitializing => "Initializing",
                DownloadPhase.dphDownloading
                    => $"Downloading ({Math.Truncate(args.Progress.CurrentUpdateBytesDownloaded / MB)} MB / {Math.Truncate(args.Progress.CurrentUpdateBytesToDownload / MB)} MB)",
                DownloadPhase.dphVerifying => "Verifying",
                _ => "Unknown",
            }
        )
        {
            ParentActivityId = cmdletProgressId,
            PercentComplete = args.Progress.CurrentUpdatePercentComplete,
        };

        writeProgress(totalProgress);
        writeProgress(updateProgress);

        if (args.Progress.CurrentUpdatePercentComplete == 100)
        {
            var result = args.Progress.GetUpdateResult(args.Progress.CurrentUpdateIndex);
            if (result.ResultCode == WUApiLib.OperationResultCode.orcFailed)
            {
                var err = ErrorRecordFactory.ErrorRecordForHResult(result.HResult, null, null);
                writeError(err);
            }
            else if (result.ResultCode == WUApiLib.OperationResultCode.orcSucceeded)
            {
                downloadCompleted(update);
            }
        }
    }

    private static void OnInstallProgress(
        IInstallationJob job,
        IInstallationProgressChangedCallbackArgs args,
        int cmdletProgressId,
        Action<ProgressRecord> writeProgress,
        Action<ErrorRecord> writeError,
        Action<WindowsUpdate> writeObject
    )
    {
        var update = job.Updates[args.Progress.CurrentUpdateIndex];

        var totalProgress = new ProgressRecord(
            cmdletProgressId,
            "Installing Windows Updates",
            "Installing"
        )
        {
            PercentComplete = (100 + args.Progress.PercentComplete) / 2,
        };

        var updateProgress = new ProgressRecord(
            cmdletProgressId + args.Progress.CurrentUpdateIndex,
            $"Installing {update.Title}",
            $"{args.Progress.CurrentUpdatePercentComplete}%"
        )
        {
            ParentActivityId = cmdletProgressId,
            PercentComplete = args.Progress.CurrentUpdatePercentComplete,
        };

        writeProgress(totalProgress);
        writeProgress(updateProgress);

        if (args.Progress.CurrentUpdatePercentComplete == 100)
        {
            var result = args.Progress.GetUpdateResult(args.Progress.CurrentUpdateIndex);
            if (result.ResultCode == WUApiLib.OperationResultCode.orcFailed)
            {
                var err = ErrorRecordFactory.ErrorRecordForHResult(result.HResult, null, null);
                writeError(err);
            }
            else if (result.ResultCode == WUApiLib.OperationResultCode.orcSucceeded)
            {
                writeObject(update.Map());
            }
        }
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

    protected override void Dispose(bool disposing)
    {
        _asyncItems.Dispose();
        base.Dispose(disposing);
    }

    private static void OnJobCompleted(
        WUApiLib.OperationResultCode orc,
        int hresult,
        Action<ErrorRecord> writeError
    )
    {
        if (orc is WUApiLib.OperationResultCode.orcFailed)
        {
            var err = ErrorRecordFactory.ErrorRecordForHResult(hresult, null, null);
            writeError(err);
        }
    }
}
