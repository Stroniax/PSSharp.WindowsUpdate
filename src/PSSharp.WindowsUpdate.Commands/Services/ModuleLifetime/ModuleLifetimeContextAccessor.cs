namespace PSSharp.WindowsUpdate.Commands;

public sealed class ModuleLifetimeContextAccessor : IModuleLifetimeContextAccessor
{
    private ModuleLifetimeContext? _context;

    public ModuleLifetimeContext? Context
    {
        get => _context;
        set
        {
            var formerly = Interlocked.CompareExchange(ref _context, value, null);
            if (value is not null && formerly is not null)
            {
                throw new InvalidOperationException(
                    "The module context has already been established."
                );
            }

            if (formerly is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    public void Dispose()
    {
        (Interlocked.Exchange(ref _context, null) as IDisposable)?.Dispose();
    }
}
