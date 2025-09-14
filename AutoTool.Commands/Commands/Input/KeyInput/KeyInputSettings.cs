using AutoTool.Core.Abstractions;
using System.ComponentModel;
using System.Windows.Input;

namespace AutoTool.Commands.Input.KeyInput
{
    /// <summary>
    /// キー入力コマンドの設定
    /// </summary>
    public sealed class KeyInputSettings : AutoToolCommandSettings
    {
        [Browsable(false)]
        new public int Version { get; init; } = 1;

        [Category("基本設定"), DisplayName("キー")]
        public Key Key { get; set; } = Key.Enter;

        [Category("基本設定"), DisplayName("Ctrl")]
        public bool Ctrl { get; set; } = false;

        [Category("基本設定"), DisplayName("Alt")]
        public bool Alt { get; set; } = false;

        [Category("基本設定"), DisplayName("Shift")]
        public bool Shift { get; set; } = false;

        [Category("詳細設定"), DisplayName("対象ウィンドウタイトル")]
        public string WindowTitle { get; set; } = string.Empty;

        [Category("詳細設定"), DisplayName("対象ウィンドウクラス名")]
        public string WindowClassName { get; set; } = string.Empty;
    }
}