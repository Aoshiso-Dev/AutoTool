using System.Runtime.CompilerServices;

namespace AutoTool.Commands.Threading;

/// <summary>
/// 関連機能の登録や初期化を行う拡張メソッドを提供します。
/// </summary>
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
