using System.Management.Automation;
using System.Runtime.ExceptionServices;

namespace PSSharp.WindowsUpdate.Commands;

/// <summary>
/// Enables cmdlets not derived from <see cref="AsyncPumpCmdlet"/> to perform async operations.
/// </summary>
public static class AsyncCmdletPump
{
    /// <summary>
    ///  Allows you to run an async delegate from an otherwise-synchronous cmdlet.
    /// <para>
    /// Do not call any of the cmdlet's APIs to read or write to the pipeline except through the
    /// <see cref="AsyncCmdletPumpContext"/> argument, or within a call to
    /// <see cref="AsyncCmdletPumpContext.InvokeAsync(Action)"/>. Calling the cmdlet's APIs
    /// from outside of the runspace thread will raise an exception.
    /// </para>
    /// </summary>
    /// <param name="action"></param>
    /// <param name="cancellationToken">A token used to cancel the request.
    /// Normally hooked up to the cmdlet's <see cref="Cmdlet.StopProcessing"/> lifecycle method.
    /// Whether or not a token is passed, on .net 8.0 and later, the token passed to
    /// <paramref name="action"/> is (also) hooked up to <see cref="System.Runtime.InteropServices.PosixSignal.SIGTERM"/>
    /// and <see cref="System.Runtime.InteropServices.PosixSignal.SIGINT"/>.
    /// </param>
    public static void Pump(
        this Cmdlet cmdlet,
        Func<AsyncCmdletPumpContext, CancellationToken, Task> action,
        CancellationToken cancellationToken = default
    )
    {
        var context = new AsyncCmdletPumpContext(cmdlet);

        Task.Run(
            async () =>
            {
                try
                {
                    await action(context, cancellationToken);
                }
                catch (Exception e)
                {
                    var edi = ExceptionDispatchInfo.Capture(e);
                    context.Enqueue(edi);
                }
                finally
                {
                    context.Close();
                }
            },
            cancellationToken
        );

        context.Run(cancellationToken);
    }
}
