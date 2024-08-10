using System.Collections;
using System.Management.Automation.Language;
using PSValueWildcard;

namespace PSSharp.WindowsUpdate.Commands;

public sealed record ArgumentCompletionContext
{
    public required string CommandName { get; init; }
    public required string ParameterName { get; init; }
    public required string WordToComplete { get; init; }
    public required CommandAst CommandAst { get; init; }
    public required IDictionary FakeBoundParameters { get; init; }
    public ReadOnlyMemory<char> Pattern { get; init; }
    public ArgumentCompletionQuotation Quotation { get; init; }

    public bool IsMatch(ReadOnlySpan<char> text)
    {
        if (Pattern.Length > 64)
        {
            var pattern = $"{Pattern}*".AsSpan();
            return ValueWildcardPattern.IsMatch(
                text,
                pattern,
                ValueWildcardOptions.InvariantIgnoreCase
            );
        }
        else if (Pattern.Length > 0)
        {
            var pattern = (Span<char>)stackalloc char[Pattern.Length + 1];
            Pattern.Span.CopyTo(pattern);
            pattern[^1] = '*';
            return ValueWildcardPattern.IsMatch(
                text,
                pattern,
                ValueWildcardOptions.InvariantIgnoreCase
            );
        }
        else
        {
            // Empty pattern
            return true;
        }
    }
}
