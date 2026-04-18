using System.Runtime.CompilerServices;

namespace AutoTool.Commands.Threading;

public static class AsyncEnumerableExtensions
{
    public static ConfiguredCancelableAsyncEnumerable<T> ConfigureAwaitFalse<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.WithCancellation(cancellationToken).ConfigureAwait(false);
    }
}
