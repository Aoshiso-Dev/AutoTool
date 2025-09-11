using AutoTool.Core.Abstractions;
using System.Windows.Input;

namespace AutoTool.Commands.Input.KeyInput
{
    /// <summary>
    /// キー入力コマンドの設定
    /// </summary>
    public sealed record KeyInputSettings : AutoToolCommandSettings
    {
        public int Version { get; init; } = 1;
        
        /// <summary>
        /// 押下するキー
        /// </summary>
        public Key Key { get; init; } = Key.Enter;

        /// <summary>
        /// Ctrlキーを同時押しするか
        /// </summary>
        public bool Ctrl { get; init; } = false;

        /// <summary>
        /// Altキーを同時押しするか
        /// </summary>
        public bool Alt { get; init; } = false;

        /// <summary>
        /// Shiftキーを同時押しするか
        /// </summary>
        public bool Shift { get; init; } = false;

        /// <summary>
        /// 対象ウィンドウのタイトル（オプション）
        /// </summary>
        public string WindowTitle { get; init; } = string.Empty;

        /// <summary>
        /// 対象ウィンドウのクラス名（オプション）
        /// </summary>
        public string WindowClassName { get; init; } = string.Empty;

        /// <summary>
        /// キー入力の説明（オプション）
        /// </summary>
        public string Description { get; init; } = string.Empty;
    }
}