namespace PSSharp.WindowsUpdate.Commands;

public interface IModuleLifetimeContextAccessor
{
    ModuleLifetimeContext? Context { get; set; }
}
