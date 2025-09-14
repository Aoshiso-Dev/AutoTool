using AutoTool.Core.Abstractions;
using System.ComponentModel;
using System.Windows.Controls;

namespace AutoTool.Commands.Input.ClickImage;

/// <summary>
/// クリックコマンドの設定
/// </summary>
public sealed class ClickImageSettings : AutoToolCommandSettings
{
    [Browsable(false)]
    new public int Version { get; init; } = 1;


    [Category("基本設定"), DisplayName("マウスボタン"),]
    public MouseButton Button { get; set; } = MouseButton.Left;

    [Category("基本設定"), DisplayName("画像パス"),]
    public string ImagePath { get; set; } = string.Empty;

    [Category("詳細設定"), DisplayName("類似度"), Description("0.0～1.0の範囲で指定。1.0に近いほど厳密に一致する必要があります。")]
    public double Similarity { get; set; } = 0.8;

    [Category("詳細設定"), DisplayName("タイムアウト"), Description("ミリ秒単位で指定。0の場合、無限に待機します。")]
    public int TimeoutMs { get; set; } = 5000;

    [Category("詳細設定"), DisplayName("検出間隔"), Description("ミリ秒単位で指定。画像の検出を試みる間隔です。")]
    public int IntervalMs { get; set; } = 100;

    [Category("詳細設定"), DisplayName("対象ウィンドウタイトル"),]
    public string WindowTitle { get; set; } = string.Empty;

    [Category("詳細設定"), DisplayName("対象ウィンドウクラス名"),]
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