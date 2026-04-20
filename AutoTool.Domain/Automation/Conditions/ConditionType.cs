namespace AutoTool.Domain.Automation.Conditions;

/// <summary>
/// 条件判定コマンドで利用する比較種別を定義し、分岐ロジックの判定基準を明確にします。
/// </summary>
public static class ConditionType
{
    public static readonly string True = "True";
    public static readonly string False = "False";
    public static readonly string ImageExists = "ImageExists";
    public static readonly string ImageNotExists = "ImageNotExists";

    private static readonly string[] Types =
    [
        True,
        False,
        ImageExists,
        ImageNotExists
    ];

    public static IEnumerable<string> GetTypes()
    {
        return Types;
    }

    public static bool IsSupported(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Types.Contains(value, StringComparer.Ordinal);
    }

    public static bool TryParse(string? value, out string conditionType)
    {
        conditionType = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();
        if (!IsSupported(normalized))
        {
            return false;
        }

        conditionType = normalized;
        return true;
    }
}
