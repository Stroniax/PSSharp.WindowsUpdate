namespace PSSharp.WindowsUpdate.Commands;

/// <summary>
/// Used to ensure that a warning message is only written the first time
/// that someone calls Download-WindowsUpdate without piping input.
/// </summary>
public sealed class DownloadPreferPipelineService
{
    private int _hasNotified = 0;

    public bool HasNotified()
    {
        return Interlocked.Exchange(ref _hasNotified, 1) == 1;
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _hasNotified, 0);
    }
}
