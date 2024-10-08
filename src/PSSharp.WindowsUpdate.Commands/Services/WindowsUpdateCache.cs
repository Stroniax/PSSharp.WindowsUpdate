using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace PSSharp.WindowsUpdate.Commands;

public sealed class WindowsUpdateCache
{
    private readonly ConcurrentDictionary<Guid, WindowsUpdate> _cache = new();

    public void Set(WindowsUpdate update)
    {
#if NET8_0_OR_GREATER
        _cache.AddOrUpdate(
            update.Identity.UpdateID,
            static (key, arg) => arg,
            static (key, existing, arg) =>
                arg.Identity.RevisionNumber > existing.Identity.RevisionNumber ? arg : existing,
            update
        );
#else
        throw new NotImplementedException();
#endif
    }

    public bool TryGetUpdate(Guid updateID, [MaybeNullWhen(false)] out WindowsUpdate update)
    {
        return _cache.TryGetValue(updateID, out update);
    }

    public IEnumerable<WindowsUpdate> GetUpdates()
    {
        return _cache.Values;
    }
}
