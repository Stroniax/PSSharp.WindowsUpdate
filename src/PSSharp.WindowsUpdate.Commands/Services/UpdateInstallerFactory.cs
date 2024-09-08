using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public sealed class UpdateInstallerFactory(WindowsUpdateSessionAccessor session)
    : IUpdateInstallerFactory
{
    private readonly WindowsUpdateSessionAccessor _session = session;

    public IUpdateInstaller CreateUpdateInstaller()
    {
        var installer = _session.GetSession().CreateUpdateInstaller();
        installer.ClientApplicationID = "PSSharp.WindowsUpdate";
        return installer;
    }
}
