using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public sealed class UpdateDownloaderFactory(WindowsUpdateSessionAccessor session)
    : IUpdateDownloaderFactory
{
    private readonly WindowsUpdateSessionAccessor _session = session;

    public IUpdateDownloader CreateUpdateDownloader()
    {
        return _session.GetSession().CreateUpdateDownloader();
    }
}
