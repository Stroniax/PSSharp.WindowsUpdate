namespace PSSharp.WindowsUpdate.Commands;

public readonly record struct ShouldContinueResult(
    bool ShouldContinue,
    bool YesToAll,
    bool NoToAll
);
