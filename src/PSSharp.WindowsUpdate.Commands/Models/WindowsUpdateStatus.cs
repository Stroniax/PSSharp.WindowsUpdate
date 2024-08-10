namespace PSSharp.WindowsUpdate.Commands;

public sealed record WindowsUpdateStatus
{
    public Version? AgentVersion { get; init; }
    public Version? ProductVersion { get; init; }
    public DateTime? LastSearchSuccessDate { get; init; }
    public DateTime? LastInstallationSuccessDate { get; init; }
    public bool IsAutomaticUpdatesEnabled { get; init; }
    public bool RebootRequired { get; init; }
}
