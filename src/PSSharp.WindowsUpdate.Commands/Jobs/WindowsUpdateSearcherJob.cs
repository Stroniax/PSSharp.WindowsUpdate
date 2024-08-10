using System.Management.Automation;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using PSValueWildcard;
using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public sealed class WindowsUpdateSearcherJob : Job
{
    private readonly IUpdateSearcher _searcher;
    private readonly string _criteria;
    private readonly string[]? _titleFilter;
    private ISearchJob? _job;

    internal WindowsUpdateSearcherJob(
        IUpdateSearcher searcher,
        string criteria,
        string[]? titleFilter
    )
    {
        PSJobTypeName = "WindowsUpdateSearchJob";
        _searcher = searcher;
        _criteria = criteria;
        _titleFilter = titleFilter;
    }

    internal void StartJob()
    {
        if (JobStateInfo.State != JobState.NotStarted)
        {
            throw new InvalidJobStateException(
                JobStateInfo.State,
                "The job has already been started."
            );
        }

        SetJobState(JobState.Running);
        _job = _searcher.BeginSearch(_criteria, JobCompletionCallback.Instance, this);
    }

    internal void OnSearchCompleted(ISearchJob job, ISearchCompletedCallbackArgs args)
    {
        // handle search completion
        ISearchResult result;
        try
        {
            result = _searcher.EndSearch(job);
        }
        catch (Exception e)
        {
            Error.Add(
                ErrorRecordFactory.CreateErrorRecord(
                    ErrorRecordFactory.CreateBestMatchException(e),
                    job,
                    ErrorRecordFactory.UpdateSearchFailure
                )
            );
            SetJobState(JobState.Failed);
            return;
        }

        foreach (IUpdateException error in result.Warnings)
        {
            Error.Add(
                ErrorRecordFactory.CreateErrorRecord(
                    new WindowsUpdateException(error),
                    error,
                    ErrorRecordFactory.UpdateSearchError
                )
            );
        }

        foreach (IUpdate update in result.Updates)
        {
            Debug.Add(new DebugRecord($"Update search found: {update.Title}."));

            if (
                _titleFilter is { Length: > 0 }
                && !_titleFilter.Any(n => ValueWildcardPattern.IsMatch(update.Title, n))
            )
            {
                Debug.Add(new DebugRecord($"Update search filtered out: {update.Title}."));
                continue;
            }

            Output.Add(PSObject.AsPSObject(new WindowsUpdate(update)));
        }

        if (result.ResultCode == WUApiLib.OperationResultCode.orcAborted)
        {
            SetJobState(JobState.Stopped);
        }
        else if (result.ResultCode == WUApiLib.OperationResultCode.orcFailed)
        {
            SetJobState(JobState.Failed);
        }
        else
        {
            SetJobState(JobState.Completed);
        }
    }

    public override bool HasMoreData => Debug.Count > 0 || Output.Count > 0 || Error.Count > 0;

    public override string Location =>
        _searcher.ServerSelection switch
        {
            WUApiLib.ServerSelection.ssDefault => "Default",
            WUApiLib.ServerSelection.ssManagedServer => "ManagedServer",
            WUApiLib.ServerSelection.ssWindowsUpdate => "WindowsUpdate",
            _ => _searcher.ServiceID,
        };

    public override string StatusMessage =>
        _job switch
        {
            null => "Not Started",
            { IsCompleted: false } => "Running",
            { IsCompleted: true } => "Finished",
        };

    public override void StopJob()
    {
        SetJobState(JobState.Stopping);
        if (_job is null)
        {
            SetJobState(JobState.Stopped);
        }
        else
        {
            _job.RequestAbort();
        }
    }

    protected override void Dispose(bool disposing)
    {
        SetJobState(JobState.Stopped);
        Interlocked.Exchange(ref _job, null)?.CleanUp();
        base.Dispose(disposing);
    }
}

file sealed class JobCompletionCallback() : ISearchCompletedCallback
{
    public static readonly JobCompletionCallback Instance = new();

    public void Invoke(ISearchJob searchJob, ISearchCompletedCallbackArgs callbackArgs)
    {
        var job = (WindowsUpdateSearcherJob)searchJob.AsyncState;
        job.OnSearchCompleted(searchJob, callbackArgs);
    }
}
