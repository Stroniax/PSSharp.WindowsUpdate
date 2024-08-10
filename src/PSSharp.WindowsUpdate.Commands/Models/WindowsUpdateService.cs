using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public sealed record WindowsUpdateService
{
    internal WindowsUpdateService(IUpdateService service) => _service = service;

    private readonly IUpdateService _service;

    public string Name => _service.Name;

    public bool IsManaged => _service.IsManaged;

    public bool IsRegisteredWithAU => _service.IsRegisteredWithAU;

    public DateTime IssueDate => _service.IssueDate;

    public bool OffersWindowsUpdates => _service.OffersWindowsUpdates;

    public IEnumerable<string> RedirectUrls => _service.RedirectUrls.Map();

    public Guid ServiceID => Guid.Parse(_service.ServiceID);

    public bool IsScanPackageService => _service.IsScanPackageService;

    public bool CanRegisterWithAU => _service.CanRegisterWithAU;

    public string ServiceUrl => _service.ServiceUrl;

    public string SetupPrefix => _service.SetupPrefix;

    public bool IsDefaultAUService => (_service as IUpdateService2)?.IsDefaultAUService ?? false;
}
