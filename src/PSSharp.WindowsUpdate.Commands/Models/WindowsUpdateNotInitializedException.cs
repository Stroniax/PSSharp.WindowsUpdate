using System.Runtime.InteropServices;

namespace PSSharp.WindowsUpdate.Commands;

public class WindowsUpdateNotInitializedException : WindowsUpdateException
{
    public WindowsUpdateNotInitializedException(COMException? innerException)
        : base("The Windows Update Agent has not been initialized.", innerException)
    {
#if NET8_0_OR_GREATER
        HResult = unchecked((int)0x80240004);
#endif
    }
}
