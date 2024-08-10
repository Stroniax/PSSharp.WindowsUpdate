namespace PSSharp.WindowsUpdate.Commands;

public class WindowsUpdateDownloadedException : WindowsUpdateException
{
    public WindowsUpdateDownloadedException()
        : this(null) { }

    public WindowsUpdateDownloadedException(string? message)
        : this(message, null) { }

    public WindowsUpdateDownloadedException(string? message, Exception? innerException)
        : base(message ?? "The Windows Update is already downloaded.", innerException)
    {
#if NET8_0_OR_GREATER
        HResult = unchecked((int)0);
#endif
    }
}
