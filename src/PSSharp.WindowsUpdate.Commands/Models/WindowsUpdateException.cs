using System.Runtime.InteropServices;
using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public class WindowsUpdateException : Exception
{
    public WindowsUpdateException(string message)
        : base(message) { }

    public WindowsUpdateException(string message, Exception? innerException)
        : base(message, innerException) { }

    internal WindowsUpdateException(IUpdateException exception)
        : base(exception.Message)
    {
#if NET8_0_OR_GREATER
        HResult = exception.HResult;
#endif
    }

    public WindowsUpdateException(
        string description,
        string message,
        uint hresult,
        Exception? innerException
    )
        : base(description, innerException)
    {
#if NET8_0_OR_GREATER
        HResult = unchecked((int)hresult);
#endif
        Data.Add("Message", message);
    }
}
