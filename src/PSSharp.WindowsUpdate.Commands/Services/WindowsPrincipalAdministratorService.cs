using System.Runtime.Versioning;
using System.Security.Principal;

namespace PSSharp.WindowsUpdate.Commands;

#if NET8_0_OR_GREATER
[SupportedOSPlatform("windows")]
#endif
public sealed class WindowsPrincipalAdministratorService : IAdministratorService
{
    private bool? _isAdministrator;
    public bool IsAdministrator => _isAdministrator ??= CheckIsAdministrator();

    private static bool CheckIsAdministrator()
    {
        using var wid = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(wid);

        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
