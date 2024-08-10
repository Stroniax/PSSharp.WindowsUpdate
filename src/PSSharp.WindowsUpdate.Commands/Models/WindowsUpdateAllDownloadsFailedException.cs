namespace PSSharp.WindowsUpdate.Commands;

public class WindowsUpdateAllDownloadsFailedException : WindowsUpdateException
{
    public WindowsUpdateAllDownloadsFailedException()
        : this(null) { }

    public WindowsUpdateAllDownloadsFailedException(string? message)
        : this(message, null) { }

    public WindowsUpdateAllDownloadsFailedException(string? message, Exception? innerException)
        : base(message ?? "All Windows Updates failed to download.", innerException) { }
}
