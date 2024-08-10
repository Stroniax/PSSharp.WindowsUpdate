using System;
using System.Collections.Generic;
using System.Text;

namespace PSSharp.WindowsUpdate.Commands;

public interface IAdministratorService
{
    public bool IsAdministrator { get; }
}
