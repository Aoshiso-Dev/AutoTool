using AutoTool.Commands.Model.Input;
using System;

namespace AutoTool.Commands.Interface;

/// <summary>
/// この設定インターフェースが提供する項目を定義し、各コマンド実装で同じ設定値を扱えるようにします。
/// </summary>
public interface ICommandSettings { }

/// <summary>
/// この設定インターフェースが提供する項目を定義し、各コマンド実装で同じ設定値を扱えるようにします。
/// </summary>
public interface IWaitImageCommandSettings : ICommandSettings
{
    string ImagePath { get; set; }
    double Threshold { get; set; }
    CommandColor? SearchColor { get; set; }
    int Timeout { get; set; }
    int Interval { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
}

/// <summary>
/// この設定インターフェースが提供する項目を定義し、各コマンド実装で同じ設定値を扱えるようにします。
/// </summary>
public interface IFindImageCommandSettings : ICommandSettings
{
    string ImagePath { get; set; }
    double Threshold { get; set; }
    CommandColor? SearchColor { get; set; }
    int Timeout { get; set; }
    int Interval { get; set; }
    bool Strict { get; set; }
    string FoundVariableName { get; set; }
    string XVariableName { get; set; }
    string YVariableName { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
}

/// <summary>
/// この設定インターフェースが提供する項目を定義し、各コマンド実装で同じ設定値を扱えるようにします。
/// </summary>
public interface IFindTextCommandSettings : ICommandSettings
{
    string TargetText { get; set; }
    bool CaseSensitive { get; set; }
    string MatchMode { get; set; } // Contains / Equals
    int X { get; set; }
    int Y { get; set; }
    int Width { get; set; }
    int Height { get; set; }
    int Timeout { get; set; }
    int Interval { get; set; }
    bool Strict { get; set; }
    double MinConfidence { get; set; }
    string FoundVariableName { get; set; }
    string TextVariableName { get; set; }
    string ConfidenceVariableName { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
    string Language { get; set; }
    string PageSegmentationMode { get; set; }
    string Whitelist { get; set; }
    string PreprocessMode { get; set; }
    string TessdataPath { get; set; }
}

/// <summary>
/// この設定インターフェースが提供する項目を定義し、各コマンド実装で同じ設定値を扱えるようにします。
/// </summary>
public interface IIfImageCommandSettings : ICommandSettings
{
    string ImagePath { get; set; }
    double Threshold { get; set; }
    CommandColor? SearchColor { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
}

/// <summary>
/// この設定インターフェースが提供する項目を定義し、各コマンド実装で同じ設定値を扱えるようにします。
/// </summary>
public interface IIfTextCommandSettings : ICommandSettings
{
    string TargetText { get; set; }
    bool CaseSensitive { get; set; }
    string MatchMode { get; set; } // Contains / Equals
    int X { get; set; }
    int Y { get; set; }
    int Width { get; set; }
    int Height { get; set; }
    double MinConfidence { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
    string Language { get; set; }
    string PageSegmentationMode { get; set; }
    string Whitelist { get; set; }
    string PreprocessMode { get; set; }
    string TessdataPath { get; set; }
}

/// <summary>
/// この設定インターフェースが提供する項目を定義し、各コマンド実装で同じ設定値を扱えるようにします。
/// </summary>
public interface IClickImageCommandSettings : ICommandSettings
{
    string ImagePath { get; set; }
    double Threshold { get; set; }
    CommandColor? SearchColor { get; set; }
    int Timeout { get; set; }
    int Interval { get; set; }
    CommandMouseButton Button { get; set; }
    int HoldDurationMs { get; set; }
    string ClickInjectionMode { get; set; }
    bool SimulateMouseMove { get; set; }
    bool RestoreCursorPositionAfterClick { get; set; }
    bool RestoreWindowZOrderAfterClick { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
}

/// <summary>
/// この設定インターフェースが提供する項目を定義し、各コマンド実装で同じ設定値を扱えるようにします。
/// </summary>
public interface IHotkeyCommandSettings : ICommandSettings
{
    bool Ctrl { get; set; }
    bool Alt { get; set; }
    bool Shift { get; set; }
    CommandKey Key { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
}

/// <summary>
/// この設定インターフェースが提供する項目を定義し、各コマンド実装で同じ設定値を扱えるようにします。
/// </summary>
public interface IClickCommandSettings : ICommandSettings
{
    CommandMouseButton Button { get; set; }
    int HoldDurationMs { get; set; }
    string ClickInjectionMode { get; set; }
    bool SimulateMouseMove { get; set; }
    bool RestoreCursorPositionAfterClick { get; set; }
    bool RestoreWindowZOrderAfterClick { get; set; }
    int X { get; set; }
    int Y { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
}

/// <summary>
/// この設定インターフェースが提供する項目を定義し、各コマンド実装で同じ設定値を扱えるようにします。
/// </summary>
public interface IWaitCommandSettings : ICommandSettings
{
    int Wait { get; set; }
}

/// <summary>
/// この設定インターフェースが提供する項目を定義し、各コマンド実装で同じ設定値を扱えるようにします。
/// </summary>
public interface ILoopCommandSettings : ICommandSettings
{
    int LoopCount { get; set; }
    ICommand? Pair { get; set; }
}

/// <summary>
/// この設定インターフェースが提供する項目を定義し、各コマンド実装で同じ設定値を扱えるようにします。
/// </summary>
public interface ILoopEndCommandSettings : ICommandSettings
{
    int LoopCount { get; set; }
    ICommand? Pair { get; set; }
}

/// <summary>
/// この設定インターフェースが提供する項目を定義し、各コマンド実装で同じ設定値を扱えるようにします。
/// </summary>
public interface IRetryCommandSettings : ICommandSettings
{
    int RetryCount { get; set; }
    int RetryInterval { get; set; }
}

/// <summary>
/// この設定インターフェースが提供する項目を定義し、各コマンド実装で同じ設定値を扱えるようにします。
/// </summary>
public interface IIfWaitImageCommandSettings : ICommandSettings
{
    string ImagePath { get; set; }
    double Threshold { get; set; }
    CommandColor? SearchColor { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
}

// Items/Commands 側の実装に合わせた定義（YOLO の ClassId 判定）
/// <summary>
/// コマンド実行や画面表示で参照する設定値を保持し、入力値を型安全に扱えるようにします。
/// </summary>

public interface IIfImageExistAISettings : ICommandSettings
{
    string ModelPath { get; set; }
    string LabelsPath { get; set; }
    string LabelName { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
    int ClassID { get; set; }
    double ConfThreshold { get; set; }
    double IoUThreshold { get; set; }
}
/// <summary>
/// この設定インターフェースが提供する項目を定義し、各コマンド実装で同じ設定値を扱えるようにします。
/// </summary>
public interface IIfImageNotExistAISettings : ICommandSettings
{
    string ModelPath { get; set; }
    string LabelsPath { get; set; }
    string LabelName { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
    int ClassID { get; set; }
    double ConfThreshold { get; set; }
    double IoUThreshold { get; set; }
}

/// <summary>
/// この設定インターフェースが提供する項目を定義し、各コマンド実装で同じ設定値を扱えるようにします。
/// </summary>
public interface IExecuteCommandSettings : ICommandSettings
{
    string ProgramPath { get; set; }
    string Arguments { get; set; }
    string WorkingDirectory { get; set; }
    bool WaitForExit { get; set; }
}

// 変数操作コマンド
/// <summary>
/// コマンド実行や画面表示で参照する設定値を保持し、入力値を型安全に扱えるようにします。
/// </summary>

public interface ISetVariableCommandSettings : ICommandSettings
{
    string Name { get; set; }
    string Value { get; set; }
}

/// <summary>
/// この設定インターフェースが提供する項目を定義し、各コマンド実装で同じ設定値を扱えるようにします。
/// </summary>
public interface IIfVariableCommandSettings : ICommandSettings
{
    string Name { get; set; }
    string Operator { get; set; }
    string Value { get; set; }
}

// AI による変数設定コマンド
/// <summary>
/// コマンド実行や画面表示で参照する設定値を保持し、入力値を型安全に扱えるようにします。
/// </summary>

public interface ISetVariableAICommandSettings : ICommandSettings
{
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
    string AIDetectMode { get; set; }
    string ModelPath { get; set; }
    string LabelsPath { get; set; }
    string LabelName { get; set; }
    double ConfThreshold { get; set; }
    double IoUThreshold { get; set; }
    string Name { get; set; }
}

// OCR による変数設定コマンド
/// <summary>
/// コマンド実行や画面表示で参照する設定値を保持し、入力値を型安全に扱えるようにします。
/// </summary>

public interface ISetVariableOCRCommandSettings : ICommandSettings
{
    string Name { get; set; }
    int X { get; set; }
    int Y { get; set; }
    int Width { get; set; }
    int Height { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
    string Language { get; set; }
    string PageSegmentationMode { get; set; }
    string Whitelist { get; set; }
    double MinConfidence { get; set; }
    string PreprocessMode { get; set; }
    string TessdataPath { get; set; }
}

// AI 画像クリックコマンド
/// <summary>
/// コマンド実行や画面表示で参照する設定値を保持し、入力値を型安全に扱えるようにします。
/// </summary>

public interface IClickImageAICommandSettings : ICommandSettings
{
    string ModelPath { get; set; }
    string LabelsPath { get; set; }
    string LabelName { get; set; }
    int ClassID { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
    double ConfThreshold { get; set; }
    double IoUThreshold { get; set; }
    CommandMouseButton Button { get; set; }
    int HoldDurationMs { get; set; }
    string ClickInjectionMode { get; set; }
    bool SimulateMouseMove { get; set; }
    bool RestoreCursorPositionAfterClick { get; set; }
    bool RestoreWindowZOrderAfterClick { get; set; }
}

// スクリーンショットコマンド
/// <summary>
/// コマンド実行や画面表示で参照する設定値を保持し、入力値を型安全に扱えるようにします。
/// </summary>

public interface IScreenshotCommandSettings : ICommandSettings
{
    string SaveDirectory { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
}
