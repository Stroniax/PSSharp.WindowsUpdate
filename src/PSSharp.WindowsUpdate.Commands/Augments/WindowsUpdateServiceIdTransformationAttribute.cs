using System.Management.Automation;
using Microsoft.Extensions.DependencyInjection;

namespace PSSharp.WindowsUpdate.Commands;

/// <summary>
/// Converts a Name to a ServiceID of a Windows Update service.
/// /// </summary>
public sealed class WindowsUpdateServiceIdTransformationAttribute : ArgumentTransformationAttribute
{
    internal IServiceProvider ServiceProvider { get; init; } = Injector.Default;

    public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
    {
        if (inputData is not string serviceNameOrId)
        {
            return inputData;
        }

        if (Guid.TryParse(serviceNameOrId, out _))
        {
            return inputData;
        }

        if (serviceNameOrId.Equals("Default", StringComparison.OrdinalIgnoreCase))
        {
            // To support ServerSelection.ssDefault which is resolved from Guid.Empty
            return "00000000-0000-0000-0000-000000000000";
        }

        using var scope = ServiceProvider.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<WindowsUpdateServiceManager>();
        return manager
            .Services.Where(s => s.Name.Equals(serviceNameOrId, StringComparison.OrdinalIgnoreCase))
            .Select(s => s.ServiceID.ToString())
            .DefaultIfEmpty(serviceNameOrId)
            .First();
    }
}
