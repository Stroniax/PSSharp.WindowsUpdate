using System.Diagnostics.CodeAnalysis;

namespace PSSharp.WindowsUpdate.Commands;

public interface IUpdateSessionCache
{
    void Add(WindowsUpdateSession session);

    IEnumerable<WindowsUpdateSession> ListUpdateSessions();

    bool TryGetById(int id, [MaybeNullWhen(false)] out WindowsUpdateSession session);

    bool TryGetByInstanceId(
        Guid instanceId,
        [MaybeNullWhen(false)] out WindowsUpdateSession session
    );
}
