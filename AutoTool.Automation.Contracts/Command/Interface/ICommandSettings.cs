using AutoTool.Commands.Model.Input;
using System;

namespace AutoTool.Commands.Interface;

public interface ICommandSettings { }

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

public interface IIfImageCommandSettings : ICommandSettings
{
    string ImagePath { get; set; }
    double Threshold { get; set; }
    CommandColor? SearchColor { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
}

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
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
}

public interface IHotkeyCommandSettings : ICommandSettings
{
    bool Ctrl { get; set; }
    bool Alt { get; set; }
    bool Shift { get; set; }
    CommandKey Key { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
}

public interface IClickCommandSettings : ICommandSettings
{
    CommandMouseButton Button { get; set; }
    int HoldDurationMs { get; set; }
    string ClickInjectionMode { get; set; }
    bool SimulateMouseMove { get; set; }
    int X { get; set; }
    int Y { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
}

public interface IWaitCommandSettings : ICommandSettings
{
    int Wait { get; set; }
}

public interface ILoopCommandSettings : ICommandSettings
{
    int LoopCount { get; set; }
    ICommand? Pair { get; set; }
}

public interface ILoopEndCommandSettings : ICommandSettings
{
    int LoopCount { get; set; }
    ICommand? Pair { get; set; }
}

public interface IRetryCommandSettings : ICommandSettings
{
    int RetryCount { get; set; }
    int RetryInterval { get; set; }
}

public interface IIfWaitImageCommandSettings : ICommandSettings
{
    string ImagePath { get; set; }
    double Threshold { get; set; }
    CommandColor? SearchColor { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
}

// Items/Commands 側の実装に合わせた定義（YOLO の ClassId 判定）
public interface IIfImageExistAISettings : ICommandSettings
{
    string ModelPath { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
    int ClassID { get; set; }
    double ConfThreshold { get; set; }
    double IoUThreshold { get; set; }
}
public interface IIfImageNotExistAISettings : ICommandSettings
{
    string ModelPath { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
    int ClassID { get; set; }
    double ConfThreshold { get; set; }
    double IoUThreshold { get; set; }
}

public interface IExecuteCommandSettings : ICommandSettings
{
    string ProgramPath { get; set; }
    string Arguments { get; set; }
    string WorkingDirectory { get; set; }
    bool WaitForExit { get; set; }
}

// 変数操作コマンド
public interface ISetVariableCommandSettings : ICommandSettings
{
    string Name { get; set; }
    string Value { get; set; }
}

public interface IIfVariableCommandSettings : ICommandSettings
{
    string Name { get; set; }
    string Operator { get; set; }
    string Value { get; set; }
}

// AI による変数設定コマンド
public interface ISetVariableAICommandSettings : ICommandSettings
{
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
    string AIDetectMode { get; set; }
    string ModelPath { get; set; }
    double ConfThreshold { get; set; }
    double IoUThreshold { get; set; }
    string Name { get; set; }
}

// OCR による変数設定コマンド
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
public interface IClickImageAICommandSettings : ICommandSettings
{
    string ModelPath { get; set; }
    int ClassID { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
    double ConfThreshold { get; set; }
    double IoUThreshold { get; set; }
    CommandMouseButton Button { get; set; }
    int HoldDurationMs { get; set; }
    string ClickInjectionMode { get; set; }
    bool SimulateMouseMove { get; set; }
}

// スクリーンショットコマンド
public interface IScreenshotCommandSettings : ICommandSettings
{
    string SaveDirectory { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
}
