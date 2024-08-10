using System.Management.Automation;

namespace PSSharp.WindowsUpdate.Commands;

public interface ISessionStateAccessor
{
    SessionState? SessionState { get; }
}
