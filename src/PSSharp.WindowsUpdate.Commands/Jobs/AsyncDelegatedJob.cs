using System.Collections.Concurrent;
using System.Management.Automation;

namespace PSSharp.WindowsUpdate.Commands;

public sealed class AsyncDelegatedJob : Job
{
    private readonly CancellationTokenSource _cts;

    /// <summary>
    /// The task that is being run by the current job.
    /// </summary>
    public Task Task { get; }
    public override bool HasMoreData => Output.Count > 0 || Progress.Count > 0 || Error.Count > 0;
    public override string Location => string.Empty;
    public override string StatusMessage => Task.Status.ToString();
    public CancellationToken CancellationToken => _cts.Token;
    private static readonly AsyncLocal<AsyncDelegatedJob?> _currentJob = new();
    public static AsyncDelegatedJob? Current => _currentJob.Value;

    private AsyncDelegatedJob(
        string jobTypeName,
        string command,
        string jobName,
        Func<AsyncDelegatedJobContext, CancellationToken, Task> work,
        CancellationToken cancellationToken
    )
        : base(command, jobName)
    {
        PSJobTypeName = jobTypeName;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var context = new AsyncDelegatedJobContext(this);
        Task = Task.Run(async () =>
        {
            try
            {
                SetJobState(JobState.Running);
                _currentJob.Value = this;
                await work(context, _cts.Token).ConfigureAwait(false);
                SetJobState(JobState.Completed);
            }
            catch (OperationCanceledException e) when (e.CancellationToken == _cts.Token)
            {
                SetJobState(JobState.Stopped);
                throw;
            }
            catch (Exception e)
            {
                Error.Add(
                    ErrorRecordFactory.CreateErrorRecord(
                        ErrorRecordFactory.CreateBestMatchException(e),
                        this,
                        "JobFailure"
                    )
                );
                SetJobState(JobState.Failed);
                throw;
            }
            finally
            {
                _currentJob.Value = null;
            }
        });
    }

    public override void StopJob()
    {
        SetJobState(JobState.Stopping);
        _cts.Cancel();
    }

    /// <summary>
    /// Queues a new job to start in the threadpool.
    ///  </summary>
    /// <param name="jobTypeName"></param>
    /// <param name="command"></param>
    /// <param name="jobName"></param>
    /// <param name="work"></param>
    /// <returns></returns>
    public static AsyncDelegatedJob Start(
        string jobTypeName,
        string command,
        string jobName,
        Func<AsyncDelegatedJobContext, CancellationToken, Task> work,
        CancellationToken cancellationToken
    )
    {
        var job = new AsyncDelegatedJob(jobTypeName, command, jobName, work, cancellationToken);
        return job;
    }
}

public sealed class AsyncDelegatedJobContext
{
    internal AsyncDelegatedJobContext(AsyncDelegatedJob job)
    {
        Job = job;
    }

    public AsyncDelegatedJob Job { get; }

    public void WriteObject(object obj)
    {
        Job.Output.Add(PSObject.AsPSObject(obj));
    }

    public void WriteProgress(ProgressRecord progress)
    {
        Job.Progress.Add(progress);
    }

    public void WriteVerbose(string message)
    {
        Job.Verbose.Add(new(message));
    }

    public void WriteError(ErrorRecord error)
    {
        Job.Error.Add(error);
    }

    /// <summary>
    /// Runs a delegated action as a child job of the current job. Returns a Task which completes
    /// when the child job is finished. The <see cref="AsyncDelegatedJob.Task"/> should be awaited
    /// by whatever process is running the parent job, so that the parent job does not complete before the
    /// child job has completed.
    /// </summary>
    /// <param name="jobTypeName"></param>
    /// <param name="command"></param>
    /// <param name="jobName"></param>
    /// <param name="work"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public AsyncDelegatedJob StartChildAsync(
        string jobTypeName,
        string command,
        string jobName,
        Func<AsyncDelegatedJobContext, CancellationToken, Task> work
    )
    {
        var job = AsyncDelegatedJob.Start(
            jobTypeName,
            command,
            jobName,
            work,
            Job.CancellationToken
        );
        Job.ChildJobs.Add(job);
        return job;
    }
}
