using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public interface IUpdateSessionFactory
{
    IUpdateSession CreateSession(WebProxy? proxy = null);
}
