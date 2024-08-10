namespace PSSharp.WindowsUpdate.Commands;

internal static class EnumerableExtensions
{
    public static IEnumerable<T> TakeUntil<T>(
        this IEnumerable<T> source,
        CancellationToken cancellationToken
    )
    {
        foreach (var item in source)
        {
            yield return item;
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }
        }
    }
}
