using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public sealed class UpdateSessionFactory : IUpdateSessionFactory
{
    public IUpdateSession CreateSession(WebProxy? proxy)
    {
        var us = new UpdateSession();
        if (proxy is not null)
        {
            us.WebProxy = proxy;
        }
        us.ClientApplicationID = "PSSharp.WindowsUpdate";
        return us;
    }
}
