using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public interface IUpdateInstallerFactory
{
    IUpdateInstaller CreateUpdateInstaller();
}
