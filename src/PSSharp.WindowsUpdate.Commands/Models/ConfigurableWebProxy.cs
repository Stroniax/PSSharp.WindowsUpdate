using System.Management.Automation;
using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public sealed class ConfigurableWebProxy
{
    internal ConfigurableWebProxy()
    {
        WebProxy = new WebProxy();
    }

    internal WebProxy WebProxy { get; }

    public string Address
    {
        get => WebProxy.Address;
        set => WebProxy.Address = value;
    }

    public IReadOnlyCollection<string>? BypassList
    {
        get => WebProxy.BypassList.Cast<string>().ToList();
        set => WebProxy.BypassList = value?.ToStringCollection();
    }

    public bool BypassProxyOnLocal
    {
        get => WebProxy.BypassProxyOnLocal;
        set => WebProxy.BypassProxyOnLocal = value;
    }

    public bool ReadOnly
    {
        get => WebProxy.ReadOnly;
    }

    public bool AutoDetect
    {
        get => WebProxy.AutoDetect;
        set => WebProxy.AutoDetect = value;
    }

    public string? UserName
    {
        get => WebProxy.UserName;
    }

    public void SetCredential(PSCredential credential)
    {
        WebProxy.UserName = credential.UserName;
        WebProxy.SetPassword(credential.GetNetworkCredential().Password);
    }

    public void SetCredential(string userName, string password)
    {
        WebProxy.UserName = userName;
        WebProxy.SetPassword(password);
    }
}
