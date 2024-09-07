namespace PSSharp.WindowsUpdate.Commands;

public sealed record WindowsUpdateCmdletContext(
    IUpdateSearcherFactory Searcher,
    IUpdateDownloaderFactory Downloader,
    IUpdateInstallerFactory Installer,
    DownloadPreferPipelineService DownloadPreferPipeline,
    WindowsUpdateCache Cache
);
