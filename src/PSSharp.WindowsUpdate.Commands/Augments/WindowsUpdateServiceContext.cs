namespace PSSharp.WindowsUpdate.Commands;

public sealed record WindowsUpdateServiceContext(
    WindowsUpdateServiceManager Manager,
    IAdministratorService Administrator
);
