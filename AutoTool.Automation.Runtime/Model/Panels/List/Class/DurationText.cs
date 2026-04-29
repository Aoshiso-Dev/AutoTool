namespace AutoTool.Automation.Runtime.Lists;

/// <summary>
/// ミリ秒で保持している時間設定を、ユーザー向けの時分秒表記に整えます。
/// </summary>
internal static class DurationText
{
    private const int MillisecondsPerSecond = 1000;
    private const int MillisecondsPerMinute = 60 * MillisecondsPerSecond;
    private const int MillisecondsPerHour = 60 * MillisecondsPerMinute;

    public static string Format(int milliseconds)
    {
        var safeMilliseconds = Math.Max(0, milliseconds);
        var hours = safeMilliseconds / MillisecondsPerHour;
        var minutes = (safeMilliseconds / MillisecondsPerMinute) % 60;
        var seconds = (safeMilliseconds % MillisecondsPerMinute) / (double)MillisecondsPerSecond;

        return hours > 0
            ? $"{hours}時間{minutes}分{FormatSeconds(seconds)}秒"
            : minutes > 0
                ? $"{minutes}分{FormatSeconds(seconds)}秒"
                : $"{FormatSeconds(seconds)}秒";
    }

    private static string FormatSeconds(double seconds)
    {
        return seconds % 1 == 0
            ? ((int)seconds).ToString()
            : seconds.ToString("0.###");
    }
}
