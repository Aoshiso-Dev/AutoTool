using AutoTool.Core.Abstractions;
using AutoTool.Core.Attributes;
using AutoTool.Core.Commands;
using AutoTool.Core.Diagnostics;
using AutoTool.Core.Utilities;
using AutoTool.Services.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AutoTool.Commands.Input.KeyInput
{
    /// <summary>
    /// 指定したキーを押下するコマンド
    /// </summary>
    [Command("KeyInput", "キー入力", IconKey = "mdi:keyboard", Category = "キーボード操作", Description = "指定したキーを押下します", Order = 25)]
    public sealed class KeyInputCommand :
        IAutoToolCommand,
        IHasSettings<KeyInputSettings>,
        IValidatableCommand
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Type => "KeyInput";
        public string DisplayName => "キー入力";
        public bool IsEnabled { get; set; } = true;

        public KeyInputSettings Settings { get; private set; }

        public IServiceProvider? _serviceProvider = null;
        private readonly ILogger<KeyInputCommand>? _logger = null;

        public KeyInputCommand(KeyInputSettings settings, IServiceProvider serviceProvider)
        {
            Settings = settings;
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = _serviceProvider.GetService(typeof(ILogger<KeyInputCommand>)) as ILogger<KeyInputCommand> ?? throw new ArgumentNullException(nameof(_logger));
        }

        public async Task<ControlFlow> ExecuteAsync(CancellationToken ct)
        {
            if (!IsEnabled) return ControlFlow.Next;

            try
            {
                // キー入力を実行
                await ExecuteKeyInputAsync(Settings.Key, Settings.Ctrl, Settings.Alt, Settings.Shift,
                    Settings.WindowTitle, Settings.WindowClassName, ct);

                return ControlFlow.Next;
            }
            catch (OperationCanceledException)
            {
                // キャンセル時は停止を返す
                return ControlFlow.Stop;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Key input failed: {Key}", Settings.Key);
                return ControlFlow.Error;
            }
        }

        /// <summary>
        /// キー入力の実行（実装はプラットフォーム固有のライブラリに依存）
        /// </summary>
        private async Task ExecuteKeyInputAsync(Key key, bool ctrl, bool alt, bool shift,
            string windowTitle, string windowClassName, CancellationToken ct)
        {

            var ui = _serviceProvider?.GetService(typeof(IUIService)) as IUIService;
            ui?.ShowToast("KeyInputCommand");

            // ログ出力
            var hotkeyText = GetHotkeyString(key, ctrl, alt, shift);

            var target = string.IsNullOrEmpty(windowTitle) 
                ? "システム全体" 
                : $"ウィンドウ「{windowTitle}」";
                
            _logger?.LogInformation("キー入力実行: {Hotkey} -> {Target}", hotkeyText, target);
        }

        /// <summary>
        /// ホットキーの文字列表現を生成
        /// </summary>
        private string GetHotkeyString(Key key, bool ctrl, bool alt, bool shift)
        {
            var parts = new List<string>();

            if (ctrl) parts.Add("Ctrl");
            if (alt) parts.Add("Alt");
            if (shift) parts.Add("Shift");
            parts.Add(key.ToString());

            return string.Join("+", parts);
        }

        public IEnumerable<string> Validate(IServiceProvider _)
        {
            if (Settings.Key == Key.None)
                yield return "有効なキーを指定してください。";

            // 危険なキーの組み合わせをチェック
            if (Settings.Ctrl && Settings.Alt && Settings.Key == Key.Delete)
                yield return "Ctrl+Alt+Deleteは危険な組み合わせです。";

            if (Settings.Alt && Settings.Key == Key.F4)
                yield return "Alt+F4はアプリケーションを終了させる可能性があります。";
        }
    }
}