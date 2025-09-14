using AutoTool.Core.Abstractions;
using System.ComponentModel;
using System.Drawing;

namespace AutoTool.Commands.Flow.IfImageExist;

public sealed class IfImageExistSettings : AutoToolCommandSettings
{
    [Browsable(false)]
    new public int Version { get; init; } = 1;

    [Category("基本設定"), DisplayName("画像ファイルパス"),]
    public string ImagePath { get; set; } = string.Empty;

    [Category("詳細設定"), DisplayName("類似度"),]
    public double Similarity { get; set; } = 0.8;

    [Category("詳細設定"), DisplayName("対象ウィンドウタイトル"),]
    public string WindowTitle { get; set; } = string.Empty;

    [Category("詳細設定"), DisplayName("対象ウィンドウクラス"),]
    public string WindowClass { get; set; } = string.Empty;
}
