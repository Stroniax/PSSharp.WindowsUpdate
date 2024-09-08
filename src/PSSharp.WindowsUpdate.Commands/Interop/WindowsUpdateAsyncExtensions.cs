using System.Runtime.InteropServices;
using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public static class WindowsUpdateAsyncExtensions
{
    public static IDownloadJob BeginDownload(
        this IUpdateDownloader downloader,
        Action<IDownloadJob, IDownloadProgressChangedCallbackArgs> progressChanged,
        Action<IDownloadJob, IDownloadCompletedCallbackArgs> downloadCompleted,
        object? state
    )
    {
        var progressChangedCallback = new DownloadProgressChangedCallback(progressChanged);
        var downloadCompletedCallback = new DownloadCompletedCallback(downloadCompleted);
        var job = downloader.BeginDownload(
            progressChangedCallback,
            downloadCompletedCallback,
            state
        );
        return job;
    }

    public static async Task<IDownloadResult> DownloadAsync(
        this IUpdateDownloader downloader,
        Action<IDownloadJob, IDownloadProgressChangedCallbackArgs> progress,
        CancellationToken cancellationToken
    )
    {
        var tcs = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        var job = downloader.BeginDownload(
            progress,
            (job, args) => ((TaskCompletionSource<bool>)job.AsyncState).TrySetResult(false),
            tcs
        );

        try
        {
            cancellationToken.Register(job.RequestAbort);

            await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            job.CleanUp();
        }

        cancellationToken.ThrowIfCancellationRequested();

        var result = downloader.EndDownload(job);

        return result;
    }

    public static IDownloadResult Download(
        this IUpdateDownloader downloader,
        Action<IDownloadJob, IDownloadProgressChangedCallbackArgs> progress,
        CancellationToken cancellationToken
    )
    {
        var mre = new ManualResetEventSlim();

        var progressChanged = new DownloadProgressChangedCallback(
            (job, args) => progress(job, args)
        );

        var downloadCompleted = new DownloadCompletedCallback(
            (job, args) => ((ManualResetEventSlim)job.AsyncState).Set()
        );

        var job = downloader.BeginDownload(progressChanged, downloadCompleted, mre);

        try
        {
            cancellationToken.Register(job.RequestAbort);

            mre.Wait(cancellationToken);
        }
        finally
        {
            job.CleanUp();
        }

        cancellationToken.ThrowIfCancellationRequested();

        var result = downloader.EndDownload(job);

        return result;
    }

    public static async Task<ISearchResult> SearchAsync(
        this IUpdateSearcher searcher,
        string criteria,
        CancellationToken cancellationToken
    )
    {
        var tcs = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        var onCompleted = new SearchCompletedCallback(
            (job, args) => ((TaskCompletionSource<bool>)job.AsyncState).TrySetResult(false)
        );

        var job = searcher.BeginSearch(criteria, onCompleted, tcs);

        try
        {
            cancellationToken.Register(job.RequestAbort);

            await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            job.CleanUp();
        }

        cancellationToken.ThrowIfCancellationRequested();

        var result = searcher.EndSearch(job);

        return result;
    }

    public static ISearchResult Search(
        this IUpdateSearcher searcher,
        string criteria,
        CancellationToken cancellationToken
    )
    {
        using var mre = new ManualResetEventSlim();

        var onCompleted = new SearchCompletedCallback(
            (job, args) => ((ManualResetEventSlim)job.AsyncState).Set()
        );

        var job = searcher.BeginSearch(criteria, onCompleted, mre);

        try
        {
            cancellationToken.Register(job.RequestAbort);

            mre.Wait(cancellationToken);
        }
        finally
        {
            job.CleanUp();
        }

        cancellationToken.ThrowIfCancellationRequested();

        var result = searcher.EndSearch(job);
        return result;
    }

    public static IInstallationJob BeginInstall(
        this IUpdateInstaller installer,
        Action<IInstallationJob, IInstallationProgressChangedCallbackArgs> progressChanged,
        Action<IInstallationJob, IInstallationCompletedCallbackArgs> installCompleted,
        object? state
    )
    {
        var progressChangedCallback = new InstallationProgressChangedCallback(progressChanged);
        var installCompletedCallback = new InstallationCompletedCallback(installCompleted);
        var job = installer.BeginInstall(progressChangedCallback, installCompletedCallback, state);
        return job;
    }

    public static async Task<IInstallationResult> InstallAsync(
        this IUpdateInstaller installer,
        Action<IInstallationJob, IInstallationProgressChangedCallbackArgs> progress,
        CancellationToken cancellationToken
    )
    {
        var tcs = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        var progressChanged = new InstallationProgressChangedCallback(
            (job, args) => progress(job, args)
        );

        var job = installer.BeginInstall(
            progress,
            (job, args) => ((TaskCompletionSource<bool>)job.AsyncState).TrySetResult(false),
            tcs
        );

        try
        {
            cancellationToken.Register(job.RequestAbort);

            await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            job.CleanUp();
        }

        cancellationToken.ThrowIfCancellationRequested();

        var result = installer.EndInstall(job);

        return result;
    }
}

file abstract class WindowsUpdateDelegate<TArg1, TArg2>(Action<TArg1, TArg2> action)
{
    private readonly Action<TArg1, TArg2> _action = action;

    public void Invoke(TArg1 arg1, TArg2 arg2)
    {
        _action(arg1, arg2);
    }
}

file sealed class DownloadProgressChangedCallback(
    Action<IDownloadJob, IDownloadProgressChangedCallbackArgs> action
)
    : WindowsUpdateDelegate<IDownloadJob, IDownloadProgressChangedCallbackArgs>(action),
        IDownloadProgressChangedCallback;

file sealed class DownloadCompletedCallback(
    Action<IDownloadJob, IDownloadCompletedCallbackArgs> action
)
    : WindowsUpdateDelegate<IDownloadJob, IDownloadCompletedCallbackArgs>(action),
        IDownloadCompletedCallback;

file sealed class SearchCompletedCallback(Action<ISearchJob, ISearchCompletedCallbackArgs> action)
    : WindowsUpdateDelegate<ISearchJob, ISearchCompletedCallbackArgs>(action),
        ISearchCompletedCallback;

file sealed class InstallationProgressChangedCallback(
    Action<IInstallationJob, IInstallationProgressChangedCallbackArgs> action
)
    : WindowsUpdateDelegate<IInstallationJob, IInstallationProgressChangedCallbackArgs>(action),
        IInstallationProgressChangedCallback;

file sealed class InstallationCompletedCallback(
    Action<IInstallationJob, IInstallationCompletedCallbackArgs> action
)
    : WindowsUpdateDelegate<IInstallationJob, IInstallationCompletedCallbackArgs>(action),
        IInstallationCompletedCallback;
