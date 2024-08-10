using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public sealed class WindowsUpdateSession
{
    private static int _nextId = 0;

    internal WindowsUpdateSession(IUpdateSession updateSession, string? name)
    {
        Session = updateSession;
        Id = Interlocked.Increment(ref _nextId);
        InstanceId = Guid.NewGuid();
        Name = name ?? $"Local";
    }

    public int Id { get; }
    public Guid InstanceId { get; }
    public string Name { get; }
    internal IUpdateSession Session { get; }
}

