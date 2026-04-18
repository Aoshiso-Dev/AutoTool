using System.Runtime.ExceptionServices;

namespace AutoTool.Core.Diagnostics;

public static class ExceptionDetailsFormatter
{
    public static string GetMostRelevantMessage(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var captured = ExceptionDispatchInfo.Capture(exception).SourceException;
        return GetExceptionChain(captured).LastOrDefault()?.Message ?? captured.Message;
    }

    public static IReadOnlyList<Exception> GetExceptionChain(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        List<Exception> chain = [];
        var current = ExceptionDispatchInfo.Capture(exception).SourceException;
        while (current is not null)
        {
            chain.Add(current);
            current = current.InnerException;
        }

        return chain;
    }

    public static string FormatDetailed(Exception exception)
    {
        var chain = GetExceptionChain(exception);
        return string.Join(" | ", chain.Select((x, i) => $"[{i}] {x.GetType().Name}: {x.Message}"));
    }
}
