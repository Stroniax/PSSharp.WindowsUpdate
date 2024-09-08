using System.Collections.Concurrent;
using System.Management.Automation;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace PSSharp.WindowsUpdate.Commands;

public sealed class AsyncCmdletPumpContext(Cmdlet cmdlet)
{
    private readonly Cmdlet _cmdlet = cmdlet;
    private readonly BlockingCollection<Action> _messages = [];

    public Task InvokeAsync(Action action)
    {
        var tcs = new TaskCompletionSource<bool>();
        _messages.Add(() =>
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

    public Task<T> InvokeAsync<T>(Func<T> func)
    {
        var tcs = new TaskCompletionSource<T>();
        _messages.Add(() =>
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

    public Task InvokeAsync<TArg>(TArg arg, Action<TArg> action)
    {
        var tcs = new TaskCompletionSource<bool>();
        _messages.Add(() =>
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

    public Task<T> InvokeAsync<TArg, T>(TArg arg, Func<TArg, T> func)
    {
        var tcs = new TaskCompletionSource<T>();
        _messages.Add(() =>
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

    internal void Enqueue(ExceptionDispatchInfo edi) => _messages.Add(edi.Throw);

    public Task WriteDebugAsync(string text) => InvokeAsync(() => _cmdlet.WriteDebug(text));

    public Task WriteVerboseAsync(string text) => InvokeAsync(() => _cmdlet.WriteVerbose(text));

    public Task WriteWarningAsync(string text) => InvokeAsync(() => _cmdlet.WriteWarning(text));

    public Task WriteErrorAsync(ErrorRecord errorRecord) =>
        InvokeAsync(() => _cmdlet.WriteError(errorRecord));

    public Task WriteObjectAsync(object sendToPipeline) =>
        InvokeAsync(() => _cmdlet.WriteObject(sendToPipeline));

    public Task WriteObjectAsync(object sendToPipeline, bool enumerateCollection) =>
        InvokeAsync(() => _cmdlet.WriteObject(sendToPipeline, enumerateCollection));

    public Task WriteProgressAsync(ProgressRecord progressRecord) =>
        InvokeAsync(() => _cmdlet.WriteProgress(progressRecord));

    public Task WriteInformationAsync(InformationRecord informationRecord) =>
        InvokeAsync(() => _cmdlet.WriteInformation(informationRecord));

    public Task<bool> ShouldProcessAsync(string target) =>
        InvokeAsync(() => _cmdlet.ShouldProcess(target));

    public Task<bool> ShouldProcessAsync(string target, string action) =>
        InvokeAsync(() => _cmdlet.ShouldProcess(target, action));

    public Task<bool> ShouldContinueAsync(string query, string caption) =>
        InvokeAsync(() => _cmdlet.ShouldContinue(query, caption));

    public Task<ShouldContinueResult> ShouldContinueAsync(
        string query,
        string caption,
        bool yesToAll = false,
        bool noToAll = false
    ) =>
        InvokeAsync(() => _cmdlet.ShouldContinue(query, caption, ref yesToAll, ref noToAll))
            .ContinueWith(t => new ShouldContinueResult(t.Result, yesToAll, noToAll));

    public void Run(CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
#if NET8_0_OR_GREATER
        using var posix = PosixSignalRegistration.Create(
            PosixSignal.SIGTERM | PosixSignal.SIGINT,
            (_) => cts.Cancel()
        );
#endif

        foreach (var message in _messages.GetConsumingEnumerable(cts.Token))
        {
            message();
        }
    }

    internal void Close() => _messages.CompleteAdding();

    public void Dispose()
    {
        _messages.Dispose();
    }
}
