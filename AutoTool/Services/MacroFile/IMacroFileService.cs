using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoTool.ViewModel.Shared;

namespace AutoTool.Services.MacroFile
{
    /// <summary>
    /// マクロファイルの読み書きサービスインターフェース
    /// </summary>
    public interface IMacroFileService
    {
        /// <summary>
        /// マクロファイルを読み込み
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <returns>読み込まれたコマンドアイテムのリスト</returns>
        Task<MacroFileResult> LoadMacroFileAsync(string filePath);

        /// <summary>
        /// マクロファイルを保存
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <param name="items">保存するコマンドアイテムのリスト</param>
        /// <param name="metadata">マクロファイルのメタデータ</param>
        /// <returns>保存結果</returns>
        Task<bool> SaveMacroFileAsync(string filePath, IEnumerable<UniversalCommandItem> items, MacroFileMetadata? metadata = null);

        /// <summary>
        /// ファイルダイアログを表示してマクロファイルを読み込み
        /// </summary>
        /// <returns>読み込み結果</returns>
        Task<MacroFileResult> ShowLoadFileDialogAsync();

        /// <summary>
        /// ファイルダイアログを表示してマクロファイルを保存
        /// </summary>
        /// <param name="items">保存するコマンドアイテム</param>
        /// <param name="currentFileName">現在のファイル名</param>
        /// <returns>保存結果</returns>
        Task<MacroFileSaveResult> ShowSaveFileDialogAsync(IEnumerable<UniversalCommandItem> items, string currentFileName = "新規ファイル");

        /// <summary>
        /// マクロファイルのメタデータを検証
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <returns>メタデータ情報</returns>
        Task<MacroFileMetadata?> ValidateMacroFileAsync(string filePath);

        /// <summary>
        /// サポートされるファイル形式かどうかを確認
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <returns>サポートされている場合はtrue</returns>
        bool IsSupportedFileFormat(string filePath);
    }

    /// <summary>
    /// マクロファイル読み込み結果
    /// </summary>
    public class MacroFileResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<UniversalCommandItem> Items { get; set; } = new();
        public MacroFileMetadata? Metadata { get; set; }
        public string? FilePath { get; set; }
    }

    /// <summary>
    /// マクロファイル保存結果
    /// </summary>
    public class MacroFileSaveResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? FilePath { get; set; }
    }

    /// <summary>
    /// マクロファイルのメタデータ
    /// </summary>
    public class MacroFileMetadata
    {
        public string Version { get; set; } = "1.0";
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
        public string Author { get; set; } = Environment.UserName;
        public List<string> Tags { get; set; } = new();
        public int CommandCount { get; set; }
    }
}