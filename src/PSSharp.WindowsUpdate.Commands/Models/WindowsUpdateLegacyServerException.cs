namespace PSSharp.WindowsUpdate.Commands;

public class WindowsUpdateLegacyServerException : WindowsUpdateException
{
    public WindowsUpdateLegacyServerException()
        : this(null) { }

    public WindowsUpdateLegacyServerException(string? message)
        : this(message, null) { }

    public WindowsUpdateLegacyServerException(string? message, Exception? innerException)
        : base(
            message
                ?? "The windows update server is using a legacy Windows Update Agent which does not support the operation.",
            innerException
        )
    {
#if NET8_0_OR_GREATER
        HResult = unchecked((int)0x802B0002);
#endif
    }
}
