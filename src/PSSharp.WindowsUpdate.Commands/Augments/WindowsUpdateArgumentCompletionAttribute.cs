using System;
using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using Microsoft.Extensions.DependencyInjection;

namespace PSSharp.WindowsUpdate.Commands;

public abstract class WindowsUpdateArgumentCompletionAttribute<TServiceContext>
    : ArgumentCompleterAttribute,
#if NET8_0_OR_GREATER
        IArgumentCompleterFactory,
#endif
        IArgumentCompleter
{
#if NET8_0_OR_GREATER
    internal WindowsUpdateArgumentCompletionAttribute()
        : base() { }
#else
    /// <summary>
    /// Call this constructor with a parameterless constructor in the derived type, passing the implementing
    /// type to this constructor. Because <c>netstandard2.0</c>'s version of PowerShell does not support
    /// <c>IArgumentCompleterFactory</c>, this constructor allows the attribute's implementation to be called
    /// for completions but note that no parameters will be passed to the implementing type's constructor.
    /// </summary>
    internal WindowsUpdateArgumentCompletionAttribute(Type implementingType)
        : base(implementingType) { }
#endif

    /// <summary>
    /// The <see cref="IServiceProvider"/> to use for dependency injection. This is the the
    /// singleton scope service provier. It is used to create a scope for argument completions.
    /// It is exposed as a mutable property for testing purposes.
    /// </summary>
    internal IServiceProvider ServiceProvider { get; init; } = Injector.Default;

    public IEnumerable<CompletionResult> CompleteArgument(
        string commandName,
        string parameterName,
        string wordToComplete,
        CommandAst commandAst,
        IDictionary fakeBoundParameters
    )
    {
        var quote = ArgumentCompletionQuotation.Capture(
            wordToComplete.AsMemory(),
            out var unquotedWordToComplete
        );

        var context = new ArgumentCompletionContext
        {
            CommandAst = commandAst,
            CommandName = commandName,
            FakeBoundParameters = fakeBoundParameters,
            ParameterName = parameterName,
            WordToComplete = wordToComplete,
            Pattern = unquotedWordToComplete,
            Quotation = quote
        };

        using var scope = ServiceProvider.CreateScope();

        using var timeout = new CancellationTokenSource();
        timeout.CancelAfter(3000);

        var serviceContext = ActivatorUtilities.CreateInstance<TServiceContext>(
            scope.ServiceProvider
        );

        var completions = new List<CompletionResult>();

        try
        {
            foreach (
                var completion in CompleteArgument(serviceContext, context, timeout.Token)
                    .TakeUntil(timeout.Token)
            )
            {
                completions.Add(completion);
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout during argument completion should not prevent the
            // existing completions from being displayed.
        }

        return completions;
    }

    public abstract IEnumerable<CompletionResult> CompleteArgument(
        TServiceContext serviceContext,
        ArgumentCompletionContext completionContext,
        CancellationToken cancellationToken
    );

    public IArgumentCompleter Create() => this;
}
