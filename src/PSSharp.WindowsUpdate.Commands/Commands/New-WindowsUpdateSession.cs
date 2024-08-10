using System.Management.Automation;
using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

[Cmdlet(VerbsCommon.New, "WindowsUpdateSession")]
public sealed class NewWindowsUpdateSessionCommand
    : WindowsUpdateCmdlet<WindowsUpdateSessionCmdletContext>
{
    /// <summary>
    /// A friendly name for the session.
    /// </summary>
    [Parameter]
    public string? Name { get; init; }

    /// <summary>
    /// A web proxy for the session.
    /// </summary>
    [Parameter]
    public ConfigurableWebProxy? WebProxy { get; init; }

    protected override void ProcessRecord(
        WindowsUpdateSessionCmdletContext context,
        CancellationToken _
    )
    {
        var sn = context.SessionFactory.CreateSession(WebProxy?.WebProxy);

        var wrapper = new WindowsUpdateSession(sn, Name);

        context.Store.Add(wrapper);

        WriteObject(wrapper);
    }
}
