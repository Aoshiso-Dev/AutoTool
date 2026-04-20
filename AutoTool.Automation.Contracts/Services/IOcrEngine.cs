namespace AutoTool.Commands.Services;

/// <summary>
/// OCR 抽出要求パラメータです。
/// </summary>
public sealed class OcrRequest
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 300;
    public int Height { get; set; } = 100;
    public string WindowTitle { get; set; } = string.Empty;
    public string WindowClassName { get; set; } = string.Empty;
    public string Language { get; set; } = "jpn";
    public string PageSegmentationMode { get; set; } = "6";
    public string Whitelist { get; set; } = string.Empty;
    public string PreprocessMode { get; set; } = "Gray";
    public string TessdataPath { get; set; } = string.Empty;
}

/// <summary>
/// OCR 抽出結果です。
/// </summary>
public readonly record struct OcrExtractionResult(string Text, double Confidence);

/// <summary>
/// OCR エンジンの抽象契約です。
/// </summary>
public interface IOcrEngine
{
    Task<OcrExtractionResult> ExtractTextAsync(
        OcrRequest request,
        CancellationToken cancellationToken);
}
