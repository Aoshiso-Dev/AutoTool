using System.Collections.Generic;
using AutoTool.Model.CommandDefinition;

namespace AutoTool.Services.UI
{
    /// <summary>
    /// EditPanel設定プロパティサービスのインターフェース
    /// </summary>
    public interface IEditPanelPropertyService
    {
        // アイテムタイプ判定プロパティ
        bool IsWaitImageItem { get; }
        bool IsClickImageItem { get; }
        bool IsClickImageAIItem { get; }
        bool IsHotkeyItem { get; }
        bool IsClickItem { get; }
        bool IsWaitItem { get; }
        bool IsLoopItem { get; }
        bool IsLoopEndItem { get; }
        bool IsLoopBreakItem { get; }
        bool IsIfImageExistItem { get; }
        bool IsIfImageNotExistItem { get; }
        bool IsIfImageExistAIItem { get; }
        bool IsIfImageNotExistAIItem { get; }
        bool IsIfEndItem { get; }
        bool IsIfVariableItem { get; }
        bool IsExecuteItem { get; }
        bool IsSetVariableItem { get; }
        bool IsSetVariableAIItem { get; }
        bool IsScreenshotItem { get; }

        /// <summary>
        /// 汎用プロパティ取得
        /// </summary>
        T? GetProperty<T>(string propertyName, T? defaultValue = default);

        /// <summary>
        /// 汎用プロパティ設定
        /// </summary>
        void SetProperty<T>(string propertyName, T value);

        /// <summary>
        /// コマンド用の設定定義を取得
        /// </summary>
        List<SettingDefinition> GetSettingDefinitions(string commandType);

        /// <summary>
        /// コマンドアイテムの設定値を適用
        /// </summary>
        void ApplySettings(UniversalCommandItem item, Dictionary<string, object?> settings);

        /// <summary>
        /// コマンドアイテムから設定値を取得
        /// </summary>
        Dictionary<string, object?> GetSettings(UniversalCommandItem item);

        /// <summary>
        /// 設定定義のソースコレクションを取得
        /// </summary>
        object[]? GetSourceCollection(string collectionName);

        /// <summary>
        /// アイテムタイプの変更処理
        /// </summary>
        void ChangeItemType(UniversalCommandItem item, string newItemType);
    }
}