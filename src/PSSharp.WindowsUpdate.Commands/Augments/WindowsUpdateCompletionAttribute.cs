using System.Management.Automation;
using PSSharp.WindowsUpdate.Commands;

namespace PSSharp.WindowsUpdate.Commands;

public sealed class WindowsUpdateCompletionAttribute
    : WindowsUpdateArgumentCompletionAttribute<WindowsUpdateCompletionContext>
{
#if NETSTANDARD2_0
    public WindowsUpdateCompletionAttribute()
        : base(typeof(WindowsUpdateCompletionAttribute)) { }
#endif

    public override IEnumerable<CompletionResult> CompleteArgument(
        WindowsUpdateCompletionContext serviceContext,
        ArgumentCompletionContext completionContext,
        CancellationToken cancellationToken
    )
    {
        Func<WindowsUpdate, string> completionTextSelector = completionContext.ParameterName switch
        {
            "UpdateID" => u => completionContext.Quotation.Apply(u.Identity.UpdateID.ToString()),
            _ => u => completionContext.Quotation.Apply(u.Title),
        };
        return serviceContext
            .Cache.GetUpdates()
            .Where(u => completionContext.IsMatch(u.Title.AsSpan()))
            .Select(u => new CompletionResult(
                completionTextSelector(u),
                $"{u.Title} ({u.Identity.UpdateID})",
                CompletionResultType.ParameterValue,
                u.Title
            ));
    }
}
