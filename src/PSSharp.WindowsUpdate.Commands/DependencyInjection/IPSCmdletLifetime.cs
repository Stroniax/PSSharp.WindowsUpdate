using System.Management.Automation;

namespace PSSharp.WindowsUpdate.Commands;

public interface IPSCmdletLifetime
{
    /// <summary>
    /// Notifies services that the cmdlet is starting.
    /// </summary>
    /// <param name="cmdlet"></param>
    void OnStart(PSCmdlet cmdlet);

    /// <summary>
    /// Notifies services that the cmdlet is being aborted.
    /// </summary>
    /// <param name="cmdlet"></param>
    void OnStop(PSCmdlet cmdlet);

    /// <summary>
    /// Notifies services that the cmdlet is gracefully completing.
    /// </summary>
    /// <param name="cmdlet"></param>
    void OnEnd(PSCmdlet cmdlet);
}
