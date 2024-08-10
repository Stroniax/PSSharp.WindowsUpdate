using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public enum ServerSelection
{
    Default = tagServerSelection.ssDefault,
    ManagedServer = tagServerSelection.ssManagedServer,
    WindowsUpdate = tagServerSelection.ssWindowsUpdate,
    Others = tagServerSelection.ssOthers,
}