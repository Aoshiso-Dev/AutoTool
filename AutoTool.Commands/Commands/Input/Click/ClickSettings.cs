using AutoTool.Core.Abstractions;

namespace AutoTool.Commands.Input.Click
{
    /// <summary>
    /// クリックコマンドの設定
    /// </summary>
    public sealed record ClickSettings : AutoToolCommandSettings
    {
        public int Version { get; init; } = 1;
        
        /// <summary>
        /// クリック座標のX位置
        /// </summary>
        public int X { get; init; } = 0;

        /// <summary>
        /// クリック座標のY位置  
        /// </summary>
        public int Y { get; init; } = 0;

        /// <summary>
        /// マウスボタンの種類
        /// </summary>
        public MouseButton Button { get; init; } = MouseButton.Left;

        /// <summary>
        /// 対象ウィンドウのタイトル（オプション）
        /// </summary>
        public string WindowTitle { get; init; } = string.Empty;

        /// <summary>
        /// 対象ウィンドウのクラス名（オプション）
        /// </summary>
        public string WindowClassName { get; init; } = string.Empty;

        /// <summary>
        /// クリックの説明（オプション）
        /// </summary>
        public string Description { get; init; } = string.Empty;
    }

    /// <summary>
    /// マウスボタンの種類
    /// </summary>
    public enum MouseButton
    {
        Left = 0,
        Right = 1,
        Middle = 2
    }
}