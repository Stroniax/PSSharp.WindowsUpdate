using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using PSValueWildcard;

namespace PSSharp.WindowsUpdate.Commands;

public sealed class PSVariableUpdateSessionCache(ISessionStateAccessor sessionState)
    : IUpdateSessionCache
{
    private readonly ISessionStateAccessor _sessionState = sessionState;

    private const string PSVARIABLE_NAME = "PSSharp.WindowsUpdate.UpdateSessionCache";

    private Dictionary<Guid, WindowsUpdateSession> GetOrCreateVariableValue()
    {
        if (_sessionState.SessionState is null)
        {
            throw new InvalidOperationException(
                "SessionState is not available from the current context."
            );
        }

        var psvar = _sessionState.SessionState.PSVariable.Get(PSVARIABLE_NAME);

        if (psvar is null)
        {
            psvar = new PSVariable(
                PSVARIABLE_NAME,
                new Dictionary<Guid, WindowsUpdateSession>(),
                ScopedItemOptions.ReadOnly
            );
            _sessionState.SessionState.PSVariable.Set(psvar);
        }

        return (Dictionary<Guid, WindowsUpdateSession>)psvar.Value;
    }

    public void Add(WindowsUpdateSession session)
    {
        GetOrCreateVariableValue().Add(session.InstanceId, session);
    }

    public IEnumerable<WindowsUpdateSession> ListUpdateSessions()
    {
        return GetOrCreateVariableValue().Values;
    }

    public bool TryGetById(int id, [MaybeNullWhen(false)] out WindowsUpdateSession session)
    {
        session = GetOrCreateVariableValue().Values.FirstOrDefault(s => s.Id == id);
        return session is not null;
    }

    public bool TryGetByInstanceId(
        Guid instanceId,
        [MaybeNullWhen(false)] out WindowsUpdateSession session
    )
    {
        return GetOrCreateVariableValue().TryGetValue(instanceId, out session);
    }
}
