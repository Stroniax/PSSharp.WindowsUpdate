namespace PSSharp.WindowsUpdate.Commands;

public readonly struct ArgumentCompletionQuotation
{
    private readonly Tag _tag;

    private ArgumentCompletionQuotation(Tag tag) => _tag = tag;

    public static ArgumentCompletionQuotation None => new(Tag.None);
    public static ArgumentCompletionQuotation SingleQuote => new(Tag.SingleQuote);
    public static ArgumentCompletionQuotation DoubleQuote => new(Tag.DoubleQuote);

    public string Apply(string value) =>
        _tag switch
        {
            Tag.DoubleQuote when value.IndexOfAny(['$', '"', '`']) != -1 => $"\"{value}\"",
            Tag.SingleQuote or Tag.DoubleQuote => $"'{value.Replace("'", "''")}'",
            _ when value.IndexOfAny([' ', '$', '"', '\'', '`']) != -1
                => $"'{value.Replace("'", "''")}'",
            _ => value,
        };

    public static ArgumentCompletionQuotation Capture(
        ReadOnlyMemory<char> value,
        out ReadOnlyMemory<char> unquotedValue
    )
    {
        switch (value.Span)
        {
            case ['"', .., '"']:
                unquotedValue = value[1..^1];
                return DoubleQuote;
            case ['"', ..]:
                unquotedValue = value[1..];
                return DoubleQuote;
            case ['\'', .., '\'']:
                unquotedValue = value[1..^1];
                return SingleQuote;
            case ['\'', ..]:
                unquotedValue = value[1..];
                return SingleQuote;
            default:
                unquotedValue = value;
                return None;
        }
    }

    public static bool operator ==(
        ArgumentCompletionQuotation left,
        ArgumentCompletionQuotation right
    ) => left._tag == right._tag;

    public static bool operator !=(
        ArgumentCompletionQuotation left,
        ArgumentCompletionQuotation right
    ) => left._tag != right._tag;

    public override bool Equals(object? obj) =>
        obj is ArgumentCompletionQuotation quotation && quotation == this;

    public override int GetHashCode() => (int)_tag;

    public override string ToString() =>
        _tag switch
        {
            Tag.None => string.Empty,
            Tag.SingleQuote => "'",
            Tag.DoubleQuote => "\"",
            _ => throw new InvalidOperationException("Invalid quotation internal state."),
        };

    private enum Tag : byte
    {
        None = 0,
        SingleQuote = 1,
        DoubleQuote = 2,
    }
}
