using AutoTool.Core.Abstractions;
using AutoTool.Core.Attributes;
using AutoTool.Core.Commands;
using AutoTool.Core.Diagnostics;
using AutoTool.Core.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AutoTool.Commands.Input.KeyInput
{
    /// <summary>
    /// 指定したキーを押下するコマンド
    /// </summary>
    [Command("keyinput", "キー入力", IconKey = "mdi:keyboard", Category = "キーボード操作", Description = "指定したキーを押下します", Order = 25)]
    public sealed class KeyInputCommand :
        IAutoToolCommand,
        IHasSettings<KeyInputSettings>,
        IValidatableCommand,
        IDeepCloneable<KeyInputCommand>
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Type => "keyinput";
        public string DisplayName => "キー入力";
        public bool IsEnabled { get; set; } = true;

        public KeyInputSettings Settings { get; private set; }

        public KeyInputCommand(KeyInputSettings settings)
        {
            Settings = settings;
        }

        public async Task<ControlFlow> ExecuteAsync(IExecutionContext ctx, CancellationToken ct)
        {
            if (!IsEnabled) return ControlFlow.Next;

            try
            {
                // キー入力を実行
                await ExecuteKeyInputAsync(ctx, Settings.Key, Settings.Ctrl, Settings.Alt, Settings.Shift,
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
                ctx.Logger.LogError(ex, "Key input failed: {Key}", Settings.Key);
                return ControlFlow.Error;
            }
        }

        /// <summary>
        /// キー入力の実行（実装はプラットフォーム固有のライブラリに依存）
        /// </summary>
        private async Task ExecuteKeyInputAsync(IExecutionContext ctx, Key key, bool ctrl, bool alt, bool shift,
            string windowTitle, string windowClassName, CancellationToken ct)
        {
            // TODO: 実際のキー入力実装
            // 既存のKeyHelper.Inputを使用する場合の例:
            // await KeyHelper.Input.KeyPressAsync(key, ctrl, alt, shift, windowTitle, windowClassName);
            
            // 現在はシミュレーション
            await Task.Delay(10, ct); // キー入力の処理時間をシミュレート
            
            // ログ出力
            var hotkeyText = GetHotkeyString(key, ctrl, alt, shift);
            
            var target = string.IsNullOrEmpty(windowTitle) 
                ? "システム全体" 
                : $"ウィンドウ「{windowTitle}」";
                
            ctx.Logger.LogInformation("キー入力実行: {Hotkey} -> {Target}", hotkeyText, target);
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

        public KeyInputCommand DeepClone()
            => new KeyInputCommand(Settings with { });

        public void ReplaceSettings(KeyInputSettings next) => Settings = next;
    }
}