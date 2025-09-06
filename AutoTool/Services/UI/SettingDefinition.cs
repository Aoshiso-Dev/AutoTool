using System;

namespace AutoTool.Services.UI
{
    /// <summary>
    /// コマンド設定項目の定義
    /// </summary>
    public class SettingDefinition
    {
        /// <summary>
        /// プロパティ名
        /// </summary>
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// 表示名
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// プロパティの型
        /// </summary>
        public Type PropertyType { get; set; } = typeof(string);

        /// <summary>
        /// デフォルト値
        /// </summary>
        public object? DefaultValue { get; set; }

        /// <summary>
        /// 説明
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 必須項目かどうか
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// 読み取り専用かどうか
        /// </summary>
        public bool IsReadOnly { get; set; } = false;

        /// <summary>
        /// 最小値（数値型の場合）
        /// </summary>
        public object? MinValue { get; set; }

        /// <summary>
        /// 最大値（数値型の場合）
        /// </summary>
        public object? MaxValue { get; set; }

        /// <summary>
        /// 選択肢のソース（コンボボックスなどで使用）
        /// </summary>
        public string? SourceCollection { get; set; }

        /// <summary>
        /// カテゴリ
        /// </summary>
        public string Category { get; set; } = "一般";

        /// <summary>
        /// 表示順序
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// エディタのタイプ（ファイル選択、色選択など）
        /// </summary>
        public string EditorType { get; set; } = "Text";

        /// <summary>
        /// バリデーションルール
        /// </summary>
        public string? ValidationRule { get; set; }

        public SettingDefinition()
        {
        }

        public SettingDefinition(string propertyName, string displayName, Type propertyType)
        {
            PropertyName = propertyName;
            DisplayName = displayName;
            PropertyType = propertyType;
        }

        public SettingDefinition(string propertyName, string displayName, Type propertyType, object? defaultValue)
            : this(propertyName, displayName, propertyType)
        {
            DefaultValue = defaultValue;
        }

        public override string ToString()
        {
            return $"{DisplayName} ({PropertyName})";
        }
    }
}