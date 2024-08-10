namespace PSSharp.WindowsUpdate.Commands;

public sealed record WindowsUpdateSessionCmdletContext(
    IUpdateSessionCache Store,
    IUpdateSessionFactory SessionFactory
);
