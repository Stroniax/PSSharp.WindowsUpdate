using System.Management.Automation;

namespace PSSharp.WindowsUpdate.Commands;

public sealed class WindowsUpdateServiceCompletionAttribute
    : WindowsUpdateArgumentCompletionAttribute<WindowsUpdateServiceContext>
{
#if NETSTANDARD2_0
    public WindowsUpdateServiceCompletionAttribute()
        : base(typeof(WindowsUpdateServiceCompletionAttribute)) { }
#endif

    public override IEnumerable<CompletionResult> CompleteArgument(
        WindowsUpdateServiceContext serviceContext,
        ArgumentCompletionContext completionContext,
        CancellationToken cancellationToken
    )
    {
        var services = serviceContext.Manager.Services.ToArray();

        foreach (var service in services)
        {
            if (
                !completionContext.IsMatch(service.ServiceID.ToString().AsSpan())
                && !completionContext.IsMatch(service.Name.AsSpan())
            )
            {
                continue;
            }

            var completionText =
                completionContext.ParameterName == "Name"
                    ? service.Name
                    : service.ServiceID.ToString();

            yield return new CompletionResult(
                completionContext.Quotation.Apply(completionText),
                $"{service.ServiceID} - {service.Name}",
                CompletionResultType.ParameterValue,
                $"{service.ServiceID} - {service.Name}"
            );
        }
    }
}
