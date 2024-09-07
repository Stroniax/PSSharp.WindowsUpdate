using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public sealed record WindowsUpdateHistoryEntry : IWindowsUpdateComObjectWrapper
{
    public WindowsUpdateHistoryEntry(IUpdateHistoryEntry entry)
    {
        _entry = entry;
    }

    private readonly IUpdateHistoryEntry _entry;
    internal IUpdateHistoryEntry Entry => _entry;

    object IWindowsUpdateComObjectWrapper.GetComObject() => _entry;

    private WindowsUpdateIdentity? _updateIdentity;
    public WindowsUpdateIdentity UpdateIdentity => _updateIdentity ??= _entry.UpdateIdentity.Map();
    public string Title => _entry.Title;
    public string Description => _entry.Description;
    public string SupportUrl => _entry.SupportUrl;

    public DateTime Date => _entry.Date.ToLocalTime();
    public int HResult => _entry.HResult;
    public UpdateOperation Operation => _entry.Operation.Map();
    public OperationResultCode ResultCode => _entry.ResultCode.Map();
    public int UnmappedResultCode => _entry.UnmappedResultCode;
    public string UninstallationNotes => _entry.UninstallationNotes;
    public IEnumerable<string> UninstallationSteps => _entry.UninstallationSteps.Map();

    public Guid? ServiceID => _entry.ServiceID is null ? null : Guid.Parse(_entry.ServiceID);
    public ServerSelection ServerSelection => _entry.ServerSelection.Map();
    public string ClientApplicationID => _entry.ClientApplicationID;

    private IEnumerable<WindowsUpdateCategory>? _categories;
    public IEnumerable<WindowsUpdateCategory>? Categories =>
        _categories ??= (_entry as IUpdateHistoryEntry2)?.Categories.Map();
}
