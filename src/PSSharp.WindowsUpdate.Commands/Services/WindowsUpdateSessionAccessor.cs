using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public sealed class WindowsUpdateSessionAccessor(
    IPSCmdletAccessor cmdlet,
    IUpdateSessionFactory factory
)
{
    private readonly IPSCmdletAccessor _cmdlet = cmdlet;
    private readonly IUpdateSessionFactory _factory = factory;

    /// <summary>
    /// Gets the <see cref="IUpdateSession"/> instance to use for current processing.
    /// </summary>
    /// <returns></returns>
    public IUpdateSession GetSession()
    {
        if (_cmdlet.Cmdlet is IWindowsUpdateSessionCmdlet { Session: not null } sessionCmdlet)
        {
            return sessionCmdlet.Session.Session;
        }
        else
        {
            return _factory.CreateSession();
        }
    }
}
