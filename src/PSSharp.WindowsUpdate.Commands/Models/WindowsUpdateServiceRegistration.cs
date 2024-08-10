using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public sealed class WindowsUpdateServiceRegistration
{
    internal WindowsUpdateServiceRegistration(IUpdateServiceRegistration registration) =>
        _registration = registration;

    private readonly IUpdateServiceRegistration _registration;

    public UpdateServiceRegistrationState RegistrationState => _registration.RegistrationState;

    public Guid ServiceID => Guid.Parse(_registration.ServiceID);

    public bool IsPendingRegistrationWithAU => _registration.IsPendingRegistrationWithAU;

    private WindowsUpdateService? _service;
    public WindowsUpdateService Service => _service ??= new(_registration.Service);
}
