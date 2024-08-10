using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public sealed record WindowsUpdate : IWindowsUpdateComObjectWrapper
{
    internal WindowsUpdate(IUpdate update)
    {
        _update = update;
    }

    private readonly IUpdate _update;

    internal IUpdate Update => _update;
    public string Title => Update.Title;
    public string Description => Update.Description;
    public string SupportUrl => Update.SupportUrl;
    public IEnumerable<string> KnowledgebaseArticles => Update.KBArticleIDs.Cast<string>();
    public IEnumerable<string> MoreInfoUrls => Update.MoreInfoUrls.Cast<string>();

    private WindowsUpdateIdentity? _identity;
    public WindowsUpdateIdentity Identity => _identity ??= new(Update.Identity);

    public bool IsDownloaded => Update.IsDownloaded;

    public bool IsInstalled => Update.IsInstalled;

    public bool IsMandatory => Update.IsMandatory;

    public bool IsUninstallable => Update.IsUninstallable;

    public bool IsBeta => Update.IsBeta;

    public IEnumerable<string> Languages => Update.Languages.Cast<string>();

    public DateTime LastDeploymentChangeTime => Update.LastDeploymentChangeTime;

    public decimal MaxDownloadSize => Update.MaxDownloadSize;

    public decimal MinDownloadSize => Update.MinDownloadSize;

    public string MsrcSeverity => Update.MsrcSeverity;

    public int RecommendedCpuSpeed => Update.RecommendedCpuSpeed;

    public int RecommendedHardDiskSpace => Update.RecommendedHardDiskSpace;

    public int RecommendedMemory => Update.RecommendedMemory;

    public UpdateType ReleaseType => Update.Type;

    public IEnumerable<string> SecurityBulletinIds => Update.SecurityBulletinIDs.Cast<string>();

    public IEnumerable<string> SupersededUpdateIds => Update.SupersededUpdateIDs.Cast<string>();

    public string UninstallationNotes => Update.UninstallationNotes;

    public IEnumerable<string> UninstallationSteps => Update.UninstallationSteps.Cast<string>();

    public bool AutoSelectOnWebSites => Update.AutoSelectOnWebSites;

    public bool EulaAccepted => Update.EulaAccepted;

    public IEnumerable<WindowsUpdate> BundledUpdates =>
        Update.BundledUpdates.Cast<IUpdate>().Select(u => new WindowsUpdate(u));

    public bool CanRequireSource => Update.CanRequireSource;

    public IEnumerable<string> Categories => Update.Categories.Cast<string>();

    public DownloadPriority DownloadPriority => Update.DownloadPriority;

    public void AcceptEula() => _update.AcceptEula();

    public object GetComObject() => Update;
}
