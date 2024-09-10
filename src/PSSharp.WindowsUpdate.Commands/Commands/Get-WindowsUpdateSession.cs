using System.Management.Automation;
using PSValueWildcard;

namespace PSSharp.WindowsUpdate.Commands;

[Cmdlet(VerbsCommon.Get, "WindowsUpdateSession", DefaultParameterSetName = "Name")]
public sealed class GetWindowsUpdateSessionCommand
    : WindowsUpdateCmdlet<WindowsUpdateSessionCmdletContext>
{
    /// <summary>
    /// The name of the Windows Update Session. Can include wildcards.
    /// </summary>
    [Parameter(Position = 0, ParameterSetName = "Name")]
    [SupportsWildcards]
    public string[] Name { get; set; } = [];

    [Parameter(Mandatory = true, ParameterSetName = "Id")]
    public int[] Id { get; set; } = [];

    [Parameter(
        Mandatory = true,
        ValueFromPipelineByPropertyName = true,
        ParameterSetName = "InstanceId"
    )]
    public Guid[] InstanceId { get; set; } = [];

    protected override void ProcessRecord(
        WindowsUpdateSessionCmdletContext context,
        CancellationToken _
    )
    {
        if (ParameterSetName == "InstanceId")
        {
            ProcessInstanceId(context.Store);
        }

        if (ParameterSetName == "Id")
        {
            ProcessId(context.Store);
        }

        if (ParameterSetName == "Name")
        {
            ProcessName(context.Store);
        }
    }

    private void ProcessInstanceId(IUpdateSessionCache store)
    {
        foreach (var id in InstanceId)
        {
            if (store.TryGetByInstanceId(id, out var session))
            {
                WriteObject(session);
            }
            else
            {
                WriteNotFoundError("InstanceId", id);
            }
        }
    }

    private void ProcessId(IUpdateSessionCache store)
    {
        foreach (var id in Id)
        {
            if (store.TryGetById(id, out var session))
            {
                WriteObject(session);
            }
            else
            {
                WriteNotFoundError("Id", id);
            }
        }
    }

    private readonly HashSet<Guid> _writtenSessions = [];

    private void ProcessName(IUpdateSessionCache store)
    {
        var exactNames = Name.Where(n => !WildcardPattern.ContainsWildcardCharacters(n));
        var missingNames = new HashSet<string>(exactNames, StringComparer.OrdinalIgnoreCase);
        foreach (var session in store.ListUpdateSessions())
        {
            if (Name.Length == 0 || Name.Any(n => ValueWildcardPattern.IsMatch(session.Name, n)))
            {
                if (_writtenSessions.Add(session.InstanceId))
                {
                    WriteObject(session);
                }
                missingNames.Remove(session.Name);
            }
        }
        foreach (var name in missingNames)
        {
            WriteNotFoundError("Name", name);
        }
    }

    private void WriteNotFoundError(string property, object value)
    {
        WriteError(ErrorRecordFactory.NotFound("WindowsUpdateSession", property, value));
    }
}
