using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.ComponentModel;
using AutoTool.Command.Base;
using AutoTool.Command.Definition;
using AutoTool.Services.Keyboard;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoTool.Command.Commands
{
    [AutoToolCommand(nameof(HotkeyCommand), typeof(HotkeyCommand))]
    public class HotkeyCommand : BaseCommand
    {
        private readonly IKeyboardService _keyboardService;

        [Category("基本設定"), DisplayName("キー")]
        public Key Key { get; set; } = Key.Enter;

        [Category("基本設定"), DisplayName("Ctrl押下")]
        public bool Ctrl { get; set; } = false;

        [Category("基本設定"), DisplayName("Alt押下")]
        public bool Alt { get; set; } = false;

        [Category("基本設定"), DisplayName("Shift押下")]
        public bool Shift { get; set; } = false;

        [Category("ウィンドウ"), DisplayName("ウィンドウタイトル")]
        public string WindowTitle { get; set; } = string.Empty;

        [Category("ウィンドウ"), DisplayName("ウィンドウクラス名")]
        public string WindowClassName { get; set; } = string.Empty;

        [Category("基本設定"), DisplayName("ホットキーテキスト")]
        public string HotkeyText { get; set; } = string.Empty;

        public HotkeyCommand(IAutoToolCommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "ホットキー";
            _keyboardService = serviceProvider?.GetService<IKeyboardService>() ?? throw new InvalidOperationException("IKeyboardServiceが利用できません");

            UpdateHotkeyText();
        }

        protected override void ValidateSettings()
        {
            if (Key == Key.None)
            {
                throw new ArgumentException("キーが未設定です。");
            }
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _keyboardService.KeyPressAsync(Key, Ctrl, Alt, Shift, WindowTitle, WindowClassName);

                var hotkeyDisplay = _keyboardService.GetHotkeyString(Key, Ctrl, Alt, Shift);
                var target = string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName)
                    ? "全画面" : $"{WindowTitle}[{WindowClassName}]";

                LogMessage($"ホットキーを送信しました: {hotkeyDisplay} -> {target}");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"ホットキー送信エラー: {ex.Message}");
                throw;
            }
        }

        private void UpdateHotkeyText()
        {
            if (_keyboardService != null)
            {
                HotkeyText = _keyboardService.GetHotkeyString(Key, Ctrl, Alt, Shift);
            }
        }
    }
}
