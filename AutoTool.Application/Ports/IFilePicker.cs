using System;

namespace AutoTool.Application.Ports;

/// <summary>
/// ファイル選択ダイアログを提供するポートです。
/// </summary>
public interface IFilePicker
{
    /// <summary>開くダイアログを表示し、選択パスを返します。</summary>
    string? OpenFile(FileDialogOptions options);
    /// <summary>保存ダイアログを表示し、選択パスを返します。</summary>
    string? SaveFile(FileDialogOptions options);
}

/// <summary>
/// ファイルダイアログ表示に必要なオプションです。
/// </summary>
public sealed record FileDialogOptions(
    string Title,
    string Filter,
    int FilterIndex,
    bool RestoreDirectory,
    string DefaultExt);

