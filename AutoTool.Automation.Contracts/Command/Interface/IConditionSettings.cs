namespace AutoTool.Commands.Interface;

/// <summary>
/// 条件評価設定の共通契約です。
/// </summary>
public interface IConditionSettings
{
}

/// <summary>
/// 画像条件評価で利用する設定契約です。
/// </summary>
public interface IImageConditionSettings : IConditionSettings
{
    string ImagePath { get; set; }
    double Threshold { get; set; }
    int Timeout { get; set; }
    int Interval { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
}
