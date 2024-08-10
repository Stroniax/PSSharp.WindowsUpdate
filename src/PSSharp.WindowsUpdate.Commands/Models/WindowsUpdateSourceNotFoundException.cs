namespace PSSharp.WindowsUpdate.Commands;

public class WindowsUpdateSourceNotFoundException(Exception? innerException)
    : WindowsUpdateException("Windows Update source not found. Is there a working network connection?", innerException) { }
