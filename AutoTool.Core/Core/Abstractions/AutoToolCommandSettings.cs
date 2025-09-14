using System.ComponentModel;

namespace AutoTool.Core.Abstractions;

public interface IAutoToolCommandSettings
{
    int Version { get; init; }
    string Description { get; init; }
}

public abstract class AutoToolCommandSettings : IAutoToolCommandSettings
{
    [Browsable(false)]
    public int Version { get; init; }

    /// <summary>
    /// 説明（オプション）
    /// </summary>
    [Browsable(false)]
    public string Description { get; init; } = string.Empty;
}