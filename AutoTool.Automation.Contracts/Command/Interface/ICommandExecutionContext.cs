using AutoTool.Commands.Model.Input;
using AutoTool.Commands.Services;

namespace AutoTool.Commands.Interface;

/// <summary>
/// コマンド実行時に必要なサービスを提供する実行コンテキストです。
/// </summary>
public interface ICommandExecutionContext
{
    /// <summary>
    /// 構成済みの時刻ソースから現在のローカル時刻を取得します。
    /// </summary>
    DateTimeOffset GetLocalNow();

    /// <summary>
    /// 進捗（0-100）を報告します。
    /// </summary>
    void ReportProgress(int progress);
    
    /// <summary>
    /// ログメッセージを出力します。
    /// </summary>
    void Log(string message);
    
    /// <summary>
    /// 変数値を取得します。
    /// </summary>
    string? GetVariable(string name);
    
    /// <summary>
    /// 変数値を設定します。
    /// </summary>
    void SetVariable(string name, string value);
    
    /// <summary>
    /// 相対パスを絶対パスへ変換します。
    /// </summary>
    string ToAbsolutePath(string relativePath);
    
    /// <summary>
    /// マウスクリックを実行します。
    /// </summary>
    Task ClickAsync(int x, int y, CommandMouseButton button, string? windowTitle = null, string? windowClassName = null, int holdDurationMs = 20, string clickInjectionMode = "MouseEvent", bool simulateMouseMove = false);
    
    /// <summary>
    /// ホットキー入力を送信します。
    /// </summary>
    Task SendHotkeyAsync(CommandKey key, bool ctrl, bool alt, bool shift, string? windowTitle = null, string? windowClassName = null);
    
    /// <summary>
    /// プログラムを実行します。
    /// </summary>
    Task ExecuteProgramAsync(string programPath, string? arguments, string? workingDirectory, bool waitForExit, CancellationToken cancellationToken);
    
    /// <summary>
    /// スクリーンショットを取得します。
    /// </summary>
    Task TakeScreenshotAsync(string filePath, string? windowTitle, string? windowClassName, CancellationToken cancellationToken);
    
    /// <summary>
    /// 画面上で画像検索を行います。
    /// </summary>
    Task<MatchPoint?> SearchImageAsync(string imagePath, double threshold, CommandColor? searchColor, string? windowTitle, string? windowClassName, CancellationToken cancellationToken);
    
    /// <summary>
    /// AI 検出モデルを初期化します。
    /// </summary>
    void InitializeAIModel(string modelPath, int inputSize = 640, bool useGpu = true);
    
    /// <summary>
    /// AI による物体検出を実行します。
    /// </summary>
    IReadOnlyList<DetectionResult> DetectAI(string? windowTitle, float confThreshold, float iouThreshold);

    /// <summary>
    /// ラベル名が指定されている場合はラベル優先でクラスIDを解決します。
    /// </summary>
    int ResolveAiClassId(string modelPath, int fallbackClassId, string? labelName, string? labelsPath);

    /// <summary>
    /// OCR で画面領域から文字を抽出します。
    /// </summary>
    Task<OcrExtractionResult> ExtractTextAsync(OcrRequest request, CancellationToken cancellationToken);
}
