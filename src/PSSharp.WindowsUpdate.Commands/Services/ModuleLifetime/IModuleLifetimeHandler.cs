namespace PSSharp.WindowsUpdate.Commands;

public interface IModuleLifetimeHandler
{
    void OnImport(ModuleLifetimeContext context);

    void OnRemove(ModuleLifetimeContext context);
}
