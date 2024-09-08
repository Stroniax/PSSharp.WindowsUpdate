using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public sealed class UpdateDownloaderFactory(WindowsUpdateSessionAccessor session)
    : IUpdateDownloaderFactory
{
    private readonly WindowsUpdateSessionAccessor _session = session;

    public IUpdateDownloader CreateUpdateDownloader()
    {
        var downloader = _session.GetSession().CreateUpdateDownloader();
        downloader.ClientApplicationID = "PSSharp.WindowsUpdate";
        return downloader;
    }
}
