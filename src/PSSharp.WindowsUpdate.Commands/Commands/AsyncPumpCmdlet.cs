using System.Collections.Concurrent;
using System.Management.Automation;
using System.Runtime.ExceptionServices;
using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

/// <summary>
/// A cmdlet that supports <c>async</c> via a message pump . This allows you to call
/// methods like <see cref="WriteObjectAsync"/> from any thread, which asynchronously
/// writes to the pipeline in a manner simlar to updating UI in applications.
/// </summary>
public abstract class AsyncPumpCmdlet : PSCmdlet, IDisposable
{
    private BlockingCollection<Action> _messageQueue = [];
    private readonly CancellationTokenSource _cts = new();

    protected virtual Task BeginProcessingAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;

    protected virtual Task ProcessRecordAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;

    protected virtual Task EndProcessingAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;

    protected sealed override void BeginProcessing() => SafeRunLifecycleAsync(BeginProcessingAsync);

    protected sealed override void ProcessRecord() => SafeRunLifecycleAsync(ProcessRecordAsync);

    protected sealed override void EndProcessing() => SafeRunLifecycleAsync(EndProcessingAsync);

    protected sealed override void StopProcessing()
    {
        _cts.Cancel();
        base.StopProcessing();
    }

    private void SafeRunLifecycleAsync(Func<CancellationToken, Task> lifecycleMethod)
    {
        _messageQueue = [];
        Task.Run(async () =>
        {
            try
            {
                await lifecycleMethod(_cts.Token);
            }
            catch (Exception e)
            {
                var edi = ExceptionDispatchInfo.Capture(e);
                _messageQueue.Add(() => edi.Throw());
            }
            finally
            {
                _messageQueue.CompleteAdding();
            }
        });
        foreach (var action in _messageQueue.GetConsumingEnumerable(_cts.Token))
        {
            action();
        }
    }

    #region `[Obsolete] new` methods
    [Obsolete(
        "Synchronous methods will fail if they are not called from the cmdlet's thread. Use WriteDebugAsync"
    )]
    protected new void WriteDebug(string text) => base.WriteDebug(text);

    [Obsolete(
        "Synchronous methods will fail if they are not called from the cmdlet's thread. Use WriteVerboseAsync"
    )]
    protected new void WriteVerbose(string text) => base.WriteVerbose(text);

    [Obsolete(
        "Synchronous methods will fail if they are not called from the cmdlet's thread. Use WriteWarningAsync"
    )]
    protected new void WriteWarning(string text) => base.WriteWarning(text);

    [Obsolete(
        "Synchronous methods will fail if they are not called from the cmdlet's thread. Use WriteErrorAsync"
    )]
    protected new void WriteError(ErrorRecord errorRecord) => base.WriteError(errorRecord);

    [Obsolete(
        "Synchronous methods will fail if they are not called from the cmdlet's thread. Use WriteObjectAsync"
    )]
    protected new void WriteObject(object sendToPipeline) => base.WriteObject(sendToPipeline);

    [Obsolete(
        "Synchronous methods will fail if they are not called from the cmdlet's thread. Use WriteObjectAsync"
    )]
    protected new void WriteObject(object sendToPipeline, bool enumerateCollection) =>
        base.WriteObject(sendToPipeline, enumerateCollection);

    [Obsolete(
        "Synchronous methods will fail if they are not called from the cmdlet's thread. Use WriteInformationAsync"
    )]
    protected new void WriteInformation(InformationRecord informationRecord) =>
        base.WriteInformation(informationRecord);

    [Obsolete(
        "Synchronous methods will fail if they are not called from the cmdlet's thread. Use WriteProgressAsync"
    )]
    protected new void WriteProgress(ProgressRecord progressRecord) =>
        base.WriteProgress(progressRecord);

    [Obsolete(
        "Synchronous methods will fail if they are not called from the cmdlet's thread. Use ShouldProcessAsync"
    )]
    protected new bool ShouldProcess(string target) => base.ShouldProcess(target);

    [Obsolete(
        "Synchronous methods will fail if they are not called from the cmdlet's thread. Use ShouldProcessAsync"
    )]
    protected new bool ShouldProcess(string target, string action) =>
        base.ShouldProcess(target, action);

    [Obsolete(
        "Synchronous methods will fail if they are not called from the cmdlet's thread. Use ShouldContinueAsync"
    )]
    protected new bool ShouldContinue(string query, string caption) =>
        base.ShouldContinue(query, caption);

    [Obsolete(
        "Synchronous methods will fail if they are not called from the cmdlet's thread. Use ShouldContinueAsync"
    )]
    protected new bool ShouldContinue(
        string query,
        string caption,
        ref bool yesToAll,
        ref bool noToAll
    ) => base.ShouldContinue(query, caption, ref yesToAll, ref noToAll);
    #endregion

    #region `async` methods
    protected Task WriteDebugAsync(string text) => InvokeAsync(text, base.WriteDebug);
    #endregion
    protected Task WriteVerboseAsync(string text) => InvokeAsync(text, base.WriteVerbose);

    protected Task WriteWarningAsync(string text) => InvokeAsync(text, base.WriteWarning);

    protected Task WriteErrorAsync(ErrorRecord errorRecord) =>
        InvokeAsync(errorRecord, base.WriteError);

    protected Task WriteObjectAsync(object? sendToPipeline) =>
        InvokeAsync(sendToPipeline, base.WriteObject);

    protected Task WriteObjectAsync(object? sendToPipeline, bool enumerateCollection) =>
        InvokeAsync(
            (sendToPipeline, enumerateCollection),
            args => base.WriteObject(args.sendToPipeline, args.enumerateCollection)
        );

    protected Task WriteProgressAsync(ProgressRecord progressRecord) =>
        InvokeAsync(progressRecord, base.WriteProgress);

    protected Task WriteInformationAsync(InformationRecord informationRecord) =>
        InvokeAsync(informationRecord, base.WriteInformation);

    protected Task<bool> ShouldProcessAsync(string target) =>
        InvokeAsync(target, base.ShouldProcess);

    protected Task<bool> ShouldProcessAsync(string target, string action) =>
        InvokeAsync((target, action), args => base.ShouldProcess(args.target, args.action));

    protected Task<bool> ShouldContinueAsync(string query, string caption) =>
        InvokeAsync((query, caption), args => base.ShouldContinue(args.query, args.caption));

    protected Task<ShouldContinueResult> ShouldContinueAsync(
        string query,
        string caption,
        bool yesToAll = false,
        bool noToAll = false
    ) =>
        InvokeAsync(
            (query, caption, yesToAll, noToAll),
            args => new ShouldContinueResult(
                base.ShouldContinue(args.query, args.caption, ref args.yesToAll, ref args.noToAll),
                args.yesToAll,
                args.noToAll
            )
        );

    protected Task InvokeAsync(Action action)
    {
        var tcs = new TaskCompletionSource<bool>();
        _messageQueue.Add(() =>
        {
            try
            {
                action();
                tcs.SetResult(true);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        });

        return tcs.Task;
    }

    protected Task<T> InvokeAsync<T>(Func<T> func)
    {
        var tcs = new TaskCompletionSource<T>();
        _messageQueue.Add(() =>
        {
            try
            {
                tcs.SetResult(func());
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        });

        return tcs.Task;
    }

    protected Task InvokeAsync<TArg>(TArg arg, Action<TArg> action)
    {
        var tcs = new TaskCompletionSource<bool>();
        _messageQueue.Add(() =>
        {
            try
            {
                action(arg);
                tcs.SetResult(true);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        });

        return tcs.Task;
    }

    protected Task<T> InvokeAsync<TArg, T>(TArg arg, Func<TArg, T> func)
    {
        var tcs = new TaskCompletionSource<T>();
        _messageQueue.Add(() =>
        {
            try
            {
                tcs.SetResult(func(arg));
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        });

        return tcs.Task;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cts.Dispose();
            _messageQueue.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    ~AsyncPumpCmdlet() => Dispose(disposing: false);
}
