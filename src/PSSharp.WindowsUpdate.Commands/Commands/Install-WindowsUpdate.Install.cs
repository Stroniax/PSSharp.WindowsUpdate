using System.Management.Automation;
using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public sealed partial class InstallWindowsUpdateCommand
{
    private void Install(
        WindowsUpdateCmdletContext context,
        WindowsUpdate update,
        CancellationToken cancellationToken
    )
    {
        var updateProgressId = Interlocked.Increment(ref s_nextProgressId);

        if (AsJob)
        {
            InstallAsJob(context, update, updateProgressId);
        }
        else
        {
            StartInstall(context, update, updateProgressId, cancellationToken);
        }
    }

    private void StartInstall(
        WindowsUpdateCmdletContext context,
        WindowsUpdate update,
        int updateProgressId,
        CancellationToken cancellationToken
    )
    {
        var installer = CreateInstaller(context, update);
        Interlocked.Increment(ref _runningCount);
        var job = installer.BeginInstall(
            (job, args) =>
                OnInstallProgress(
                    job,
                    args,
                    updateProgressId,
                    _asyncItems.Add,
                    _asyncItems.Add,
                    _asyncItems.Add
                ),
            (job, args) =>
            {
                try
                {
                    var result = installer.EndInstall(job);
                    OnInstallJobCompleted(result, update, _asyncItems.Add);
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

    private void InstallAsJob(
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
                await InstallAsChildJob(context, update, jobContext, updateProgressId);
            },
            CancellationToken.None
        );
        WriteObject(job);
        JobRepository.Add(job);
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

                var installResult = await installer.InstallAsync(
                    (job, args) =>
                        OnInstallProgress(
                            job,
                            args,
                            updateProgressId,
                            installJobContext.WriteProgress,
                            installJobContext.WriteError,
                            installJobContext.WriteObject
                        ),
                    installToken
                );
                OnInstallJobCompleted(installResult, update, jobContext.WriteError);
            }
        );

        await installJob.Task;
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

    private void OnInstallProgress(
        IInstallationJob job,
        IInstallationProgressChangedCallbackArgs args,
        int updateProgressId,
        Action<ProgressRecord> writeProgress,
        Action<ErrorRecord> writeError,
        Action<WindowsUpdate> writeObject
    )
    {
        var progress = CreateInstallProgress(job, args, updateProgressId);
        writeProgress(progress);

        if (args.Progress.CurrentUpdatePercentComplete == 100)
        {
            OnInstallCompleted(job, args, u => writeObject(u.Map()), writeError);
        }
    }

    private static void OnInstallCompleted(
        IInstallationJob job,
        IInstallationProgressChangedCallbackArgs args,
        Action<IUpdate> installed,
        Action<ErrorRecord> writeError
    )
    {
        var singleUpdateResult = args.Progress.GetUpdateResult(args.Progress.CurrentUpdateIndex);
        var update = job.Updates[args.Progress.CurrentUpdateIndex];

        if (singleUpdateResult.ResultCode == WUApiLib.OperationResultCode.orcSucceeded)
        {
            installed(update);
        }
        else
        {
            var updateError = ErrorRecordFactory.ErrorRecordForHResult(
                singleUpdateResult.HResult,
                update.Map(),
                null
            );
            writeError(updateError);
        }
    }

    private static void OnInstallJobCompleted(
        IInstallationResult installResult,
        WindowsUpdate update,
        Action<ErrorRecord> writeError
    )
    {
        if (installResult.ResultCode != WUApiLib.OperationResultCode.orcSucceeded)
        {
            var err = ErrorRecordFactory.ErrorRecordForHResult(installResult.HResult, update, null);
            writeError(err);
        }
    }

    private ProgressRecord CreateInstallProgress(
        IInstallationJob job,
        IInstallationProgressChangedCallbackArgs args,
        int updateProgressId
    )
    {
        var update = job.Updates[args.Progress.CurrentUpdateIndex];
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
}
