using System.Management.Automation;
using Microsoft.Extensions.DependencyInjection;

namespace PSSharp.WindowsUpdate.Commands;

public abstract class WindowsUpdateCmdlet : PSCmdlet, IDisposable
{
    public IServiceProvider ServiceProvider { get; init; } = Injector.Default;

    private readonly IServiceScope _scope;
    private IServiceScope? _processRecordScope;

    /// <summary>
    /// Indicates that a new ServiceScope should be created for each call to <see cref="ProcessRecord"/>.
    /// </summary>
    protected virtual bool UseNewServiceScopeForProcessRecord => false;

    public WindowsUpdateCmdlet()
    {
        _scope = ServiceProvider.CreateScope();
    }

    protected IServiceProvider ScopedServiceProvider =>
        _processRecordScope?.ServiceProvider ?? _scope.ServiceProvider;

    protected override void BeginProcessing()
    {
        ScopedServiceProvider.GetRequiredService<IPSCmdletLifetime>().OnStart(this);
        base.BeginProcessing();
    }

    protected override void ProcessRecord()
    {
        if (UseNewServiceScopeForProcessRecord)
        {
            _processRecordScope = _scope.ServiceProvider.CreateScope();
        }

        try
        {
            base.ProcessRecord();
        }
        finally
        {
            _processRecordScope?.Dispose();
            _processRecordScope = null;
        }
    }

    protected override void EndProcessing()
    {
        base.EndProcessing();
        ScopedServiceProvider.GetRequiredService<IPSCmdletLifetime>().OnEnd(this);
    }

    protected override void StopProcessing()
    {
        base.StopProcessing();
        ScopedServiceProvider.GetRequiredService<IPSCmdletLifetime>().OnStop(this);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope.Dispose();
        }
    }

    ~WindowsUpdateCmdlet()
    {
        Dispose(false);
    }
}
