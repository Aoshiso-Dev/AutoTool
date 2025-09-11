using System;

namespace AutoTool.Core.Attributes
{
    /// <summary>
    /// コマンドクラスに付与してメタデータを定義するAttribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class CommandAttribute : Attribute
    {
        /// <summary>
        /// コマンドタイプ（例: "wait", "click"）
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// 表示名（例: "待機", "クリック"）
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// アイコンキー（任意）
        /// </summary>
        public string? IconKey { get; set; }

        /// <summary>
        /// カテゴリ（任意）
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// 説明（任意）
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 優先度（表示順序用、小さい値が先）
        /// </summary>
        public int Order { get; set; } = 100;

        public CommandAttribute(string type, string displayName)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        }
    }
}