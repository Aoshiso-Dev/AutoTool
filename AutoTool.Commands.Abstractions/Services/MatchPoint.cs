namespace AutoTool.Commands.Services;

/// <summary>
/// 画像マッチングの結果座標
/// </summary>
/// <param name="X">X座標</param>
/// <param name="Y">Y座標</param>
public readonly record struct MatchPoint(int X, int Y);
