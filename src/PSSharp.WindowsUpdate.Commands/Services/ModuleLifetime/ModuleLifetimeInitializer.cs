using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using Microsoft.Extensions.DependencyInjection;

namespace PSSharp.WindowsUpdate.Commands;

public class ModuleLifetimeInitializer : IModuleAssemblyInitializer
{
    private IServiceProvider? _localServiceProvider;

    public virtual IServiceProvider ServiceProvider
    {
        get => _localServiceProvider ??= Injector.Default;
        set =>
            _localServiceProvider =
                value ?? throw new ArgumentNullException(nameof(ServiceProvider));
    }

    /// <remarks>
    /// This method is not called on the same instance as <see cref="OnRemove"/> during the regular
    /// lifetime because it is hooked into the PowerShell engine's process which instantiates a new
    /// instance for each action.
    /// </remarks>
    public void OnImport()
    {
        using var scope = ServiceProvider.CreateScope();

        var accessor = scope.ServiceProvider.GetRequiredService<IModuleLifetimeContextAccessor>();
        var context = new ModuleLifetimeContextImpl { ServiceProvider = ServiceProvider };
        try
        {
            accessor.Context = context;

            foreach (var handler in scope.Handlers())
            {
                handler.OnImport(context);
                context.ModuleRemoved.ThrowIfCancellationRequested();
            }
        }
        catch
        {
            context.Dispose();
            throw;
        }
    }

    /// <remarks>
    /// This method is not called on the same instance as <see cref="OnImport"/> during the regular
    /// lifetime because it is hooked into the PowerShell engine's process which instantiates a new
    /// instance for each action.
    /// </remarks>
    public void OnRemove()
    {
        using var scope = ServiceProvider.CreateScope();

        var accessor = scope.ServiceProvider.GetRequiredService<IModuleLifetimeContextAccessor>();
        var context = accessor.Context;

        context?.MaybeCancel();

        if (context is null)
        {
            throw new InvalidOperationException("The module context has not been established.");
        }

        try
        {
            foreach (var handler in scope.Handlers())
            {
                handler.OnRemove(context);
            }
        }
        finally
        {
            accessor.Context = null;
            (context as IDisposable)?.Dispose();
        }
    }
}

file sealed record ModuleLifetimeContextImpl : ModuleLifetimeContext, IDisposable
{
    private readonly CancellationTokenSource _cts = new();

    public override CancellationToken ModuleRemoved => _cts.Token;

    internal void OnModuleRemoved() => _cts.Cancel();

    public void Dispose()
    {
        _cts.Dispose();
    }
}

file static class Extensions
{
    public static IEnumerable<IModuleLifetimeHandler> Handlers(this IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IEnumerable<IModuleLifetimeHandler>>();

    public static void MaybeCancel(this ModuleLifetimeContext? context)
    {
        if (context is ModuleLifetimeContextImpl impl)
        {
            impl.OnModuleRemoved();
        }
    }
}
