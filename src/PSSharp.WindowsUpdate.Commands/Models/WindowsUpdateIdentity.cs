using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public sealed record WindowsUpdateIdentity
{
    private readonly IUpdateIdentity _identity;
    internal IUpdateIdentity Identity => _identity;

    internal WindowsUpdateIdentity(IUpdateIdentity identity)
    {
        _identity = identity;
    }

    public Guid UpdateID => Guid.Parse(_identity.UpdateID);
    public int RevisionNumber => _identity.RevisionNumber;
}
