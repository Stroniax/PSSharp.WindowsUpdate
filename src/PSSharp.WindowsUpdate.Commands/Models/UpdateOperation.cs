using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public enum UpdateOperation
{
    Installation = tagUpdateOperation.uoInstallation,
    Uninstallation = tagUpdateOperation.uoUninstallation,
}
