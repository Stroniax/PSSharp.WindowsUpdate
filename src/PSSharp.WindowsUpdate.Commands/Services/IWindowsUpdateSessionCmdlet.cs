namespace PSSharp.WindowsUpdate.Commands;

public interface IWindowsUpdateSessionCmdlet
{
    WindowsUpdateSession? Session { get; }
}
