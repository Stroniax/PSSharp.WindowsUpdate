using Microsoft.Extensions.DependencyInjection;
using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

internal static class Injector
{
    public static IServiceProvider Default { get; } = CreateDefault().BuildServiceProvider();

    public static IServiceCollection CreateDefault()
    {
        return new ServiceCollection()
            .AddLifetime()
            .AddWindowsUpdateSession()
            .AddSingleton<DownloadPreferPipelineService>()
            .AddScoped<ISessionStateAccessor, PSCmdletSessionStateAccessor>()
            .AddTransient<IAdministratorService, WindowsPrincipalAdministratorService>();
    }
}

file static class Extensions
{
    public static IServiceCollection AddLifetime(this IServiceCollection services)
    {
        return services
            // Module
            .AddSingleton<IModuleLifetimeContextAccessor, ModuleLifetimeContextAccessor>()
            // Cmdlet
            .AddScoped<IPSCmdletLifetime, PSCmdletLifetime>()
            .AddTransient<IPSCmdletLifetimeHandler, PSCmdletAccessorLifetimeHandler>()
            .AddScoped<IPSCmdletAccessor, PSCmdletAccessor>();
    }

    public static IServiceCollection AddWindowsUpdateSession(this IServiceCollection services)
    {
        return services
            .AddScoped<WindowsUpdateSessionAccessor>()
            .AddTransient<IUpdateSessionFactory, UpdateSessionFactory>()
            .AddTransient<IUpdateSessionCache, PSVariableUpdateSessionCache>()
            .AddTransient<IUpdateSearcherFactory, UpdateSearcherFactory>()
            .AddTransient<IUpdateDownloaderFactory, UpdateDownloaderFactory>()
            .AddTransient<IUpdateInstallerFactory, UpdateInstallerFactory>()
            .AddTransient(s => new WindowsUpdateServiceManager(new UpdateServiceManager()));
    }
}
