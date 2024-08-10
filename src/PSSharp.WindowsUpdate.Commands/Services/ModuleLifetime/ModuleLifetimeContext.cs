namespace PSSharp.WindowsUpdate.Commands;

public abstract record ModuleLifetimeContext
{
    public required IServiceProvider ServiceProvider { get; init; }
    public abstract CancellationToken ModuleRemoved { get; }
}
