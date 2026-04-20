using AutoTool.Commands.Model.Input;
using AutoTool.Commands.Interface;

namespace AutoTool.Automation.Contracts.Lists;

/// <summary>
/// コマンド一覧の基本項目が満たす共通契約を定義します。
/// </summary>
public interface ICommandListItem
{
    public bool IsEnable { get; set; }
    public int LineNumber { get; set; }
    public bool IsRunning { get; set; }
    public bool IsSelected { get; set; }
    public string ItemType { get; set; }
    public string Description { get; set; }
    public string Comment { get; set; }
    public int NestLevel { get; set; }
    public bool IsInLoop { get; set; }
    public bool IsInIf { get; set; }
    public int Progress { get; set; }

    ICommandListItem Clone();
    
    /// <summary>
    /// コマンド処理を実行します（必要に応じて派生側でオーバーライドします）。
    /// </summary>
    ValueTask<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        => ValueTask.FromResult(true);
}

/// <summary>
/// Wait Image 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface IWaitImageItem : ICommandListItem
{
    public string ImagePath { get; set; }
    public double Threshold { get; set; }
    public CommandColor? SearchColor { get; set; }
    public int Timeout { get; set; }
    public int Interval { get; set; }
    public string WindowTitle { get; set; }
    public string WindowClassName { get; set; }
}

/// <summary>
/// Find Image 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface IFindImageItem : ICommandListItem
{
    public string ImagePath { get; set; }
    public double Threshold { get; set; }
    public CommandColor? SearchColor { get; set; }
    public int Timeout { get; set; }
    public int Interval { get; set; }
    public bool Strict { get; set; }
    public string FoundVariableName { get; set; }
    public string XVariableName { get; set; }
    public string YVariableName { get; set; }
    public string WindowTitle { get; set; }
    public string WindowClassName { get; set; }
}

/// <summary>
/// Find Text 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface IFindTextItem : ICommandListItem
{
    public string TargetText { get; set; }
    public bool CaseSensitive { get; set; }
    public string MatchMode { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Timeout { get; set; }
    public int Interval { get; set; }
    public bool Strict { get; set; }
    public double MinConfidence { get; set; }
    public string FoundVariableName { get; set; }
    public string TextVariableName { get; set; }
    public string ConfidenceVariableName { get; set; }
    public string WindowTitle { get; set; }
    public string WindowClassName { get; set; }
    public string Language { get; set; }
    public string PageSegmentationMode { get; set; }
    public string Whitelist { get; set; }
    public string PreprocessMode { get; set; }
    public string TessdataPath { get; set; }
}

/// <summary>
/// Click Image 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface IClickImageItem : ICommandListItem
{
    public string ImagePath { get; set; }
    public double Threshold { get; set; }
    public CommandColor? SearchColor { get; set; }
    public int Timeout { get; set; }
    public int Interval { get; set; }
    public CommandMouseButton Button { get; set; }
    public int HoldDurationMs { get; set; }
    public string ClickInjectionMode { get; set; }
    public bool SimulateMouseMove { get; set; }
    public string WindowTitle { get; set; }
    public string WindowClassName { get; set; }
}

/// <summary>
/// Hotkey 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface IHotkeyItem : ICommandListItem
{
    public bool Ctrl { get; set; }
    public bool Alt { get; set; }
    public bool Shift { get; set; }
    public CommandKey Key { get; set; }
    public string WindowTitle { get; set; }
    public string WindowClassName { get; set; }
}

/// <summary>
/// Click 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface IClickItem : ICommandListItem
{
    public CommandMouseButton Button { get; set; }
    public int HoldDurationMs { get; set; }
    public string ClickInjectionMode { get; set; }
    public bool SimulateMouseMove { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public string WindowTitle { get; set; }
    public string WindowClassName { get; set; }
}

/// <summary>
/// Wait 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface IWaitItem : ICommandListItem
{
    public int Wait { get; set; }
}

/// <summary>
/// If 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface IIfItem : ICommandListItem
{
    public ICommandListItem? Pair { get; set; }
}

/// <summary>
/// If Image Exist 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface IIfImageExistItem : ICommandListItem, IIfItem
{
    public string ImagePath { get; set; }
    public double Threshold { get; set; }
    public CommandColor? SearchColor { get; set; }
    public string WindowTitle { get; set; }
    public string WindowClassName { get; set; }
}

/// <summary>
/// If Image Not Exist 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface IIfImageNotExistItem : ICommandListItem, IIfItem
{
    public string ImagePath { get; set; }
    public double Threshold { get; set; }
    public CommandColor? SearchColor { get; set; }
    public string WindowTitle { get; set; }
    public string WindowClassName { get; set; }
}

/// <summary>
/// If Text Exist 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface IIfTextExistItem : ICommandListItem, IIfItem
{
    public string TargetText { get; set; }
    public bool CaseSensitive { get; set; }
    public string MatchMode { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double MinConfidence { get; set; }
    public string WindowTitle { get; set; }
    public string WindowClassName { get; set; }
    public string Language { get; set; }
    public string PageSegmentationMode { get; set; }
    public string Whitelist { get; set; }
    public string PreprocessMode { get; set; }
    public string TessdataPath { get; set; }
}

/// <summary>
/// If Text Not Exist 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface IIfTextNotExistItem : ICommandListItem, IIfItem
{
    public string TargetText { get; set; }
    public bool CaseSensitive { get; set; }
    public string MatchMode { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double MinConfidence { get; set; }
    public string WindowTitle { get; set; }
    public string WindowClassName { get; set; }
    public string Language { get; set; }
    public string PageSegmentationMode { get; set; }
    public string Whitelist { get; set; }
    public string PreprocessMode { get; set; }
    public string TessdataPath { get; set; }
}

/// <summary>
/// If End 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface IIfEndItem : ICommandListItem
{
    public ICommandListItem? Pair { get; set; }
}

/// <summary>
/// Loop 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface ILoopItem : ICommandListItem
{
    public int LoopCount { get; set; }
    public ICommandListItem? Pair { get; set; }
}

/// <summary>
/// Loop End 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface ILoopEndItem : ICommandListItem
{
    public ICommandListItem? Pair { get; set; }
}

/// <summary>
/// Loop Break 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface ILoopBreakItem : ICommandListItem
{
}

/// <summary>
/// Retry 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface IRetryItem : ICommandListItem
{
    public int RetryCount { get; set; }
    public int RetryInterval { get; set; }
    public ICommandListItem? Pair { get; set; }
}

/// <summary>
/// Retry End 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface IRetryEndItem : ICommandListItem
{
    public ICommandListItem? Pair { get; set; }
}

/// <summary>
/// If Image Exist AI 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface IIfImageExistAIItem : ICommandListItem
{
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
    string ModelPath { get; set; }
    int ClassID { get; set; }
    double ConfThreshold { get; set; }
    double IoUThreshold { get; set; }
}
/// <summary>
/// If Image Not Exist AI 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface IIfImageNotExistAIItem : ICommandListItem
{
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
    string ModelPath { get; set; }
    int ClassID { get; set; }
    double ConfThreshold { get; set; }
    double IoUThreshold { get; set; }
}

/// <summary>
/// Click Image AI 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface IClickImageAIItem : ICommandListItem
{
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
    string ModelPath { get; set; }
    int ClassID { get; set; }
    double ConfThreshold { get; set; }
    double IoUThreshold { get; set; }
    CommandMouseButton Button { get; set; }
    int HoldDurationMs { get; set; }
    string ClickInjectionMode { get; set; }
    bool SimulateMouseMove { get; set; }
}

/// <summary>
/// Execute 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface IExecuteItem : ICommandListItem
{
    public string ProgramPath { get; set; }
    public string Arguments { get; set; }
    public string WorkingDirectory { get; set; }
    public bool WaitForExit { get; set; }
}

/// <summary>
/// Set Variable 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface ISetVariableItem : ICommandListItem
{
    public string Name { get; set; }
    public string Value { get; set; }
}

/// <summary>
/// Set Variable AI 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface ISetVariableAIItem : ICommandListItem
{
    string WindowTitle { get; set; }
    string AIDetectMode { get; set; }
    string WindowClassName { get; set; }
    string ModelPath { get; set; }
    double ConfThreshold { get; set; }
    double IoUThreshold { get; set; }
    public string Name { get; set; }
}

/// <summary>
/// Set Variable OCR 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface ISetVariableOCRItem : ICommandListItem
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

/// <summary>
/// If Variable 項目が保持する設定値と表示情報の契約を定義します。
/// </summary>
public interface IIfVariableItem : ICommandListItem, IIfItem
{
    public string Name { get; set; }
    public string Operator { get; set; }
    public string Value { get; set; }
}

// パネル編集で使用するスクリーンショット項目のインターフェース
/// <summary>
/// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
/// </summary>

public interface IScreenshotItem : ICommandListItem
{
    string SaveDirectory { get; set; }
    string WindowTitle { get; set; }
    string WindowClassName { get; set; }
}
