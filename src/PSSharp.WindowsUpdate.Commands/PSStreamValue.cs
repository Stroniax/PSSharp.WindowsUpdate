using System.Management.Automation;

namespace PSSharp.WindowsUpdate.Commands;

/// <summary>
/// An item that can be written to one of the PowerShell streams.
/// </summary>
public abstract class PSStreamValue
{
    public abstract void Match<TState>(
        TState state,
        Action<TState, object> output,
        Action<TState, ProgressRecord>? progress,
        Action<TState, ErrorRecord> error
    );

    public void Match(
        Action<object> output,
        Action<ProgressRecord>? progress,
        Action<ErrorRecord> error
    )
    {
        Match(
            (output, progress, error),
            static (state, obj) => state.output(obj),
            static (state, progress) => state.progress?.Invoke(progress),
            static (state, error) => state.error(error)
        );
    }

    public TResult Switch<TState, TResult>(
        TState state,
        Func<TState, object, TResult> output,
        Func<TState, ProgressRecord, TResult>? progress,
        Func<TState, ErrorRecord, TResult> error
    )
    {
        var result = default(TResult);
        Match(
            state,
            (state, obj) => result = output(state, obj),
            progress is null ? null : (state, pr) => result = progress(state, pr),
            (state, er) => result = error(state, er)
        );

        return result!;
    }

    public TResult Switch<TResult>(
        Func<object, TResult> output,
        Func<ProgressRecord, TResult> progress,
        Func<ErrorRecord, TResult> error
    )
    {
        return Switch(
            (output, progress, error),
            static (state, obj) => state.output(obj),
            static (state, progress) => state.progress.Invoke(progress),
            static (state, error) => state.error(error)
        );
    }

    public static PSStreamValue Output(object value) => new PSStreamOutput(value);

    public static PSStreamValue Progress(ProgressRecord progressRecord) =>
        new PSStreamProgress(progressRecord);

    public static PSStreamValue Error(ErrorRecord errorRecord) => new PSStreamError(errorRecord);

    public static implicit operator PSStreamValue(PSObject value) => Output(value);

    public static implicit operator PSStreamValue(ProgressRecord progressRecord) =>
        Progress(progressRecord);

    public static implicit operator PSStreamValue(ErrorRecord errorRecord) => Error(errorRecord);
}

file sealed class PSStreamOutput(object value) : PSStreamValue
{
    public object Value { get; } = value;

    public override void Match<TState>(
        TState state,
        Action<TState, object> output,
        Action<TState, ProgressRecord>? progress,
        Action<TState, ErrorRecord> error
    )
    {
        output.Invoke(state, Value);
    }
}

file sealed class PSStreamProgress(ProgressRecord progressRecord) : PSStreamValue
{
    public ProgressRecord ProgressRecord { get; } = progressRecord;

    public override void Match<TState>(
        TState state,
        Action<TState, object> output,
        Action<TState, ProgressRecord>? progress,
        Action<TState, ErrorRecord> error
    )
    {
        progress?.Invoke(state, ProgressRecord);
    }
}

file sealed class PSStreamError(ErrorRecord errorRecord) : PSStreamValue
{
    public ErrorRecord ErrorRecord { get; } = errorRecord;

    public override void Match<TState>(
        TState state,
        Action<TState, object> output,
        Action<TState, ProgressRecord>? progress,
        Action<TState, ErrorRecord> error
    )
    {
        error.Invoke(state, ErrorRecord);
    }
}
