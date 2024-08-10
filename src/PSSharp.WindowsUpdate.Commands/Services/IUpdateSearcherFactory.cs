using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public interface IUpdateSearcherFactory
{
    IUpdateSearcher CreateUpdateSearcher();
}
