using System;

namespace AutoTool.ViewModel.Shared
{
    /// <summary>
    /// コマンド表示用のアイテムクラス（Phase 3対応）
    /// </summary>
    public class CommandDisplayItem
    {
        public string TypeName { get; init; } = string.Empty;      // 内部で使用する英語名
        public string DisplayName { get; init; } = string.Empty;   // UI表示用の日本語名
        public string Category { get; init; } = string.Empty;      // カテゴリ名

        /// <summary>
        /// デバッグ用の文字列表示（DisplayNameではなくTypeNameを返す）
        /// </summary>
        public override string ToString() => $"{DisplayName} ({TypeName})";
        
        public override bool Equals(object? obj)
        {
            return obj is CommandDisplayItem other && TypeName == other.TypeName;
        }
        
        public override int GetHashCode()
        {
            return TypeName.GetHashCode();
        }
    }
}