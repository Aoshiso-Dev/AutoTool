using AutoTool.Core.Abstractions;
using System.ComponentModel;

namespace AutoTool.Commands.Input.Click;

/// <summary>
/// クリックコマンドの設定
/// </summary>
public sealed class ClickSettings : AutoToolCommandSettings
{
    [Browsable(false)]
    new public int Version { get; init; } = 1;

    [Category("基本設定"), DisplayName("クリック座標"),]
    public System.Windows.Point Point { get; set; } = new System.Windows.Point(0, 0);

    [Category("基本設定"), DisplayName("マウスボタン"),]
    public MouseButton Button { get; set; } = MouseButton.Left;

    [Category("基本設定"), DisplayName("対象ウィンドウタイトル"),]
    public string WindowTitle { get; set; } = string.Empty;

    [Category("基本設定"), DisplayName("対象ウィンドウクラス名"),]
    public string WindowClassName { get; set; } = string.Empty;
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