namespace AutoTool.Application.Ports;

/// <summary>
/// パネル編集で使う各種ファイル/フォルダ選択ダイアログを提供するポートです。
/// </summary>
public interface IPanelDialogService
{
    /// <summary>画像ファイルを選択します。</summary>
    string? SelectImageFile();
    /// <summary>モデルファイルを選択します。</summary>
    string? SelectModelFile();
    /// <summary>AIラベルファイルを選択します。</summary>
    string? SelectLabelFile();
    /// <summary>指定したフィルターでファイルを選択します。</summary>
    string? SelectFile(string filter);
    /// <summary>実行ファイルを選択します。</summary>
    string? SelectExecutableFile();
    /// <summary>フォルダを選択します。</summary>
    string? SelectFolder();
}
