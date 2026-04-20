namespace AutoTool.Application.Ports;

/// <summary>
/// キャプチャ保存先パスを生成するポートです。
/// </summary>
public interface ICapturePathProvider
{
    /// <summary>
    /// 新しいキャプチャファイル用のパスを返します。
    /// </summary>
    string CreateCaptureFilePath();
}
