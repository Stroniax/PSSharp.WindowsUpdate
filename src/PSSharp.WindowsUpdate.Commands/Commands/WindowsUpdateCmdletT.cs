using System.Management.Automation;
using Microsoft.Extensions.DependencyInjection;

namespace PSSharp.WindowsUpdate.Commands;

public abstract class WindowsUpdateCmdlet<TServiceContext> : WindowsUpdateCmdlet
{
    private readonly CancellationTokenSource _cancellation = new();

    protected virtual void BeginProcessing(
        TServiceContext context,
        CancellationToken cancellationToken
    ) { }

    protected virtual void ProcessRecord(
        TServiceContext context,
        CancellationToken cancellationToken
    ) { }

    protected virtual void EndProcessing(
        TServiceContext context,
        CancellationToken cancellationToken
    ) { }

    protected sealed override void BeginProcessing()
    {
        base.BeginProcessing();
        var context = ActivatorUtilities.CreateInstance<TServiceContext>(ScopedServiceProvider);
        BeginProcessing(context, _cancellation.Token);
    }

    protected sealed override void ProcessRecord()
    {
        base.ProcessRecord();
        var context = ActivatorUtilities.CreateInstance<TServiceContext>(ScopedServiceProvider);
        try
        {
            ProcessRecord(context, _cancellation.Token);
        }
        catch (PipelineStoppedException) { }
        catch (OperationCanceledException) { }
    }

    protected sealed override void EndProcessing()
    {
        base.EndProcessing();
        var context = ActivatorUtilities.CreateInstance<TServiceContext>(ScopedServiceProvider);
        EndProcessing(context, _cancellation.Token);
    }

    protected override void StopProcessing()
    {
        _cancellation.Cancel();
        base.StopProcessing();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancellation.Dispose();
        }
        base.Dispose(disposing);
    }
}
