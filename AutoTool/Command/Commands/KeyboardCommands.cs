using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoTool.Command.Base;
using AutoTool.Command.Definition;
using AutoTool.Command.Interface;
using AutoTool.Services.Keyboard;
using Microsoft.Extensions.DependencyInjection;

namespace AutoTool.Command.Commands
{
    public interface IHotkeyCommand : AutoTool.Command.Interface.ICommand 
    {
        Key Key { get; set; }
        bool Ctrl { get; set; }
        bool Alt { get; set; }
        bool Shift { get; set; }
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
        string HotkeyText { get; set; }
    }

    /// <summary>
    /// ホットキーコマンド（DI対応）
    /// </summary>
    [DirectCommand(DirectCommandRegistry.CommandTypes.Hotkey, "ホットキー", "Keyboard", "ホットキーを送信します")]
    public class HotkeyCommand : BaseCommand, IHotkeyCommand
    {
        private readonly IKeyboardService _keyboardService;

        [SettingProperty("キー", SettingControlType.KeyPicker,
            description: "送信するキー",
            category: "基本設定",
            sourceCollection: "Keys",
            isRequired: true,
            defaultValue: Key.Enter)]
        public Key Key { get; set; } = Key.Enter;
        public bool Ctrl { get; set; } = false;
        public bool Alt { get; set; } = false;
        public bool Shift { get; set; } = false;
        /*
        [SettingProperty("Ctrlキー", SettingControlType.CheckBox,
            description: "Ctrlキーを同時に押すかどうか",
            category: "修飾キー",
            defaultValue: false)]
        public bool Ctrl { get; set; } = false;

        [SettingProperty("Altキー", SettingControlType.CheckBox,
            description: "Altキーを同時に押すかどうか",
            category: "修飾キー",
            defaultValue: false)]
        public bool Alt { get; set; } = false;

        [SettingProperty("Shiftキー", SettingControlType.CheckBox,
            description: "Shiftキーを同時に押すかどうか",
            category: "修飾キー",
            defaultValue: false)]
        public bool Shift { get; set; } = false;
        */
        [SettingProperty("ウィンドウタイトル", SettingControlType.WindowPicker,
            description: "対象ウィンドウのタイトル",
            category: "ウィンドウ")]
        public string WindowTitle { get; set; } = string.Empty;

        [SettingProperty("ウィンドウクラス名", SettingControlType.TextBox,
            description: "対象ウィンドウのクラス名",
            category: "ウィンドウ")]
        public string WindowClassName { get; set; } = string.Empty;

        [SettingProperty("ホットキーテキスト", SettingControlType.TextBox,
            description: "ホットキーの表示テキスト",
            category: "表示")]
        public string HotkeyText { get; set; } = string.Empty;

        public HotkeyCommand(AutoTool.Command.Interface.ICommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "ホットキー";
            _keyboardService = serviceProvider?.GetService<IKeyboardService>() ?? throw new InvalidOperationException("IKeyboardServiceが見つかりません");
            
            // HotkeyTextを自動生成
            UpdateHotkeyText();
        }

        protected override void ValidateSettings()
        {
            // Key は None を許容しない
            if (Key == Key.None)
            {
                throw new ArgumentException("キーを指定してください。");
            }
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _keyboardService.KeyPressAsync(
                    Key, Ctrl, Alt, Shift,
                    WindowTitle, WindowClassName);

                var hotkeyDisplay = _keyboardService.GetHotkeyString(Key, Ctrl, Alt, Shift);
                var target = string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName)
                    ? "グローバル" : $"{WindowTitle}[{WindowClassName}]";

                LogMessage($"ホットキーを実行しました: {hotkeyDisplay} -> {target}");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"ホットキー実行エラー: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// プロパティ変更時にHotkeyTextを更新
        /// </summary>
        private void UpdateHotkeyText()
        {
            if (_keyboardService != null)
            {
                HotkeyText = _keyboardService.GetHotkeyString(Key, Ctrl, Alt, Shift);
            }
        }
    }

    /// <summary>
    /// テキスト入力コマンド（追加機能）
    /// </summary>
    [DirectCommand(DirectCommandRegistry.CommandTypes.TypeText, "テキスト入力", "Keyboard", "テキストを入力します")]
    public class TypeTextCommand : BaseCommand
    {
        private readonly IKeyboardService _keyboardService;

        [SettingProperty("入力テキスト", SettingControlType.TextBox,
            description: "入力するテキスト",
            category: "基本設定",
            isRequired: true)]
        public string Text { get; set; } = string.Empty;

        [SettingProperty("ウィンドウタイトル", SettingControlType.WindowPicker,
            description: "対象ウィンドウのタイトル",
            category: "ウィンドウ")]
        public string WindowTitle { get; set; } = string.Empty;

        [SettingProperty("ウィンドウクラス名", SettingControlType.TextBox,
            description: "対象ウィンドウのクラス名",
            category: "ウィンドウ")]
        public string WindowClassName { get; set; } = string.Empty;

        [SettingProperty("入力間隔", SettingControlType.NumberBox,
            description: "文字間の入力間隔（ミリ秒）",
            category: "詳細設定",
            defaultValue: 10)]
        public int InputInterval { get; set; } = 10;

        public TypeTextCommand(AutoTool.Command.Interface.ICommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "テキスト入力";
            _keyboardService = serviceProvider?.GetService<IKeyboardService>() ?? throw new InvalidOperationException("IKeyboardServiceが見つかりません");
        }

        protected override void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(Text))
            {
                throw new ArgumentException("入力テキストが空です。テキストを指定してください。");
            }

            if (InputInterval < 0)
            {
                throw new ArgumentException("入力間隔は0以上で指定してください。");
            }
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(Text))
                {
                    LogMessage("入力テキストが空です");
                    return false;
                }

                if (string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName))
                {
                    await _keyboardService.TypeTextAsync(Text);
                    LogMessage($"テキストを入力しました: \"{Text}\" (グローバル)");
                }
                else
                {
                    await _keyboardService.TypeTextAsync(Text, WindowTitle, WindowClassName);
                    LogMessage($"テキストを入力しました: \"{Text}\" -> {WindowTitle}[{WindowClassName}]");
                }

                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"テキスト入力エラー: {ex.Message}");
                throw;
            }
        }
    }
}