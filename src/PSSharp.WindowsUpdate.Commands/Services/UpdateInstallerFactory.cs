using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public sealed class UpdateInstallerFactory(WindowsUpdateSessionAccessor session)
    : IUpdateInstallerFactory
{
    private readonly WindowsUpdateSessionAccessor _session = session;

    public IUpdateInstaller CreateUpdateInstaller()
    {
        return _session.GetSession().CreateUpdateInstaller();
    }
}
