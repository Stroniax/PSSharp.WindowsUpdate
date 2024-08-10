using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public sealed class UpdateSearcherFactory(WindowsUpdateSessionAccessor session)
    : IUpdateSearcherFactory
{
    private readonly WindowsUpdateSessionAccessor _session = session;

    public IUpdateSearcher CreateUpdateSearcher()
    {
        return _session.GetSession().CreateUpdateSearcher();
    }
}
