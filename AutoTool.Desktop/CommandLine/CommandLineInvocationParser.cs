using System.Globalization;
using System.IO;

namespace AutoTool.Desktop.CommandLine;

/// <summary>
/// コマンドライン引数を <see cref="CommandLineInvocation"/> へ変換します。
/// </summary>
public static class CommandLineInvocationParser
{
    public static CommandLineParseResult Parse(IReadOnlyList<string>? args)
    {
        if (args is null || args.Count == 0)
        {
            return CommandLineParseResult.Success(CommandLineInvocation.Empty);
        }

        string? macroPath = null;
        var start = false;
        var stop = false;
        var exit = false;
        var exitOnComplete = false;
        var hide = false;
        var show = false;
        var silentErrors = false;

        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            if (string.IsNullOrWhiteSpace(arg))
            {
                continue;
            }

            if (arg.StartsWith("-", StringComparison.Ordinal))
            {
                var option = arg.ToLower(CultureInfo.InvariantCulture);
                switch (option)
                {
                    case "-macro":
                        if (!TryReadValue(args, ref i, out var macroValue))
                        {
                            return CommandLineParseResult.Failure("-macro にはファイルパスの指定が必要です。");
                        }

                        if (!TryAssignMacroPath(ref macroPath, macroValue, out var macroError))
                        {
                            return CommandLineParseResult.Failure(macroError);
                        }
                        break;
                    case "-start":
                        start = true;
                        break;
                    case "-stop":
                        stop = true;
                        break;
                    case "-exit":
                        exit = true;
                        break;
                    case "-exit-on-complete":
                        exitOnComplete = true;
                        break;
                    case "-hide":
                        hide = true;
                        break;
                    case "-show":
                        show = true;
                        break;
                    case "-silent-errors":
                        silentErrors = true;
                        break;
                    default:
                        return CommandLineParseResult.Failure($"未対応の引数です: {arg}");
                }

                continue;
            }

            if (!TryAssignMacroPath(ref macroPath, arg, out var positionalError))
            {
                return CommandLineParseResult.Failure(positionalError);
            }
        }

        if (start && stop)
        {
            return CommandLineParseResult.Failure("-start と -stop は同時に指定できません。");
        }

        if (exit && exitOnComplete)
        {
            return CommandLineParseResult.Failure("-exit と -exit-on-complete は同時に指定できません。");
        }

        if (hide && show)
        {
            return CommandLineParseResult.Failure("-hide と -show は同時に指定できません。");
        }

        if (exitOnComplete && !start)
        {
            return CommandLineParseResult.Failure("-exit-on-complete は -start と同時に指定してください。");
        }

        return CommandLineParseResult.Success(new CommandLineInvocation(
            MacroPath: macroPath,
            Start: start,
            Stop: stop,
            Exit: exit,
            ExitOnComplete: exitOnComplete,
            Hide: hide,
            Show: show,
            SilentErrors: silentErrors));
    }

    private static bool TryReadValue(IReadOnlyList<string> args, ref int currentIndex, out string value)
    {
        var nextIndex = currentIndex + 1;
        if (nextIndex >= args.Count || string.IsNullOrWhiteSpace(args[nextIndex]))
        {
            value = string.Empty;
            return false;
        }

        value = args[nextIndex];
        currentIndex = nextIndex;
        return true;
    }

    private static bool TryAssignMacroPath(ref string? macroPath, string candidate, out string error)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            error = "マクロファイルのパスが空です。";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(macroPath))
        {
            error = "マクロファイルは 1 つだけ指定してください。";
            return false;
        }

        try
        {
            macroPath = Path.GetFullPath(candidate);
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            error = $"マクロファイルのパスが不正です: {ex.Message}";
            return false;
        }
    }
}

/// <summary>
/// コマンドライン解析結果を表します。
/// </summary>
public sealed record CommandLineParseResult(CommandLineInvocation? Invocation, string? ErrorMessage)
{
    public bool IsSuccess => string.IsNullOrWhiteSpace(ErrorMessage) && Invocation is not null;

    public static CommandLineParseResult Success(CommandLineInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        return new CommandLineParseResult(invocation, null);
    }

    public static CommandLineParseResult Failure(string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(errorMessage);
        return new CommandLineParseResult(null, errorMessage);
    }
}
