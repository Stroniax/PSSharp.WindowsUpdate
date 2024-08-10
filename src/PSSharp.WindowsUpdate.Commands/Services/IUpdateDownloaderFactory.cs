using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public interface IUpdateDownloaderFactory
{
    IUpdateDownloader CreateUpdateDownloader();
}
