using System.Management.Automation;
using System.Runtime.InteropServices;
using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public sealed class WindowsUpdateServiceManager
{
    internal WindowsUpdateServiceManager(IUpdateServiceManager2 manager) => _manager = manager;

    private readonly IUpdateServiceManager2 _manager;

    public IEnumerable<WindowsUpdateService> Services =>
        _manager.Services.Cast<IUpdateService2>().Select(s => new WindowsUpdateService(s));

    public WindowsUpdateServiceRegistration AddService(
        string serviceID,
        int flags,
        string? authorizationCabPath
    )
    {
        var service = _manager.AddService2(serviceID, flags, authorizationCabPath);
        return new WindowsUpdateServiceRegistration(service);
    }

    public void RegisterServiceWithAU(string serviceID)
    {
        _manager.RegisterServiceWithAU(serviceID);
    }

    public void RemoveService(string serviceID)
    {
        _manager.RemoveService(serviceID);
    }

    public void UnregisterServiceWithAU(string serviceID)
    {
        _manager.UnregisterServiceWithAU(serviceID);
    }

    public WindowsUpdateService AddScanPackageService(
        string serviceName,
        string scanFileLocation,
        int flags = 0
    )
    {
        var service = _manager.AddScanPackageService(serviceName, scanFileLocation, flags);
        return new WindowsUpdateService((IUpdateService2)service);
    }

    public void SetOption(string optionName, object optionValue)
    {
        _manager.SetOption(optionName, optionValue);
    }

    public string ClientApplicationId
    {
        get => _manager.ClientApplicationID;
        set => _manager.ClientApplicationID = value;
    }

    public WindowsUpdateServiceRegistration QueryServiceRegistration(string serviceID)
    {
        var registration = _manager.QueryServiceRegistration(serviceID);
        return new WindowsUpdateServiceRegistration(registration);
    }

    public WindowsUpdateServiceRegistration AddService2(
        string ServiceID,
        int flags,
        string authorizationCabPath
    )
    {
        var registration = _manager.AddService2(ServiceID, flags, authorizationCabPath);
        return new WindowsUpdateServiceRegistration(registration);
    }
}

public static class WindowsUpdateServiceManagerExtensions
{
    public static void SetAllowedServiceID(this WindowsUpdateServiceManager manager, Guid serviceID)
    {
        try
        {
            manager.SetOption("AllowedServiceID", serviceID.ToString());
        }
        catch (COMException e) when (e.HResult == unchecked((int)0x80240036))
        {
            throw new InvalidOperationException(
                "The computer is not allowed to access the update site.",
                e
            );
        }
    }

    public static void SetAllowWarningUI(this WindowsUpdateServiceManager manager, bool allowed)
    {
        try
        {
            manager.SetOption("AllowWarningUI", allowed);
        }
        catch (COMException e) when (e.HResult == unchecked((int)0x80240036))
        {
            throw new InvalidOperationException(
                "The computer is not allowed to access the update site.",
                e
            );
        }
    }
}
