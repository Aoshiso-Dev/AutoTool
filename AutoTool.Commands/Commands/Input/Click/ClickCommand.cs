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

namespace AutoTool.Commands.Input.Click
{
    /// <summary>
    /// 指定した座標でマウスクリックを実行するコマンド
    /// </summary>
    [Command("click", "クリック", IconKey = "mdi:cursor-default-click", Category = "マウス操作", Description = "指定した座標でマウスクリックを実行します", Order = 20)]
    public sealed class ClickCommand :
        IAutoToolCommand,
        IHasSettings<ClickSettings>,
        IValidatableCommand,
        IDeepCloneable<ClickCommand>
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Type => "click";
        public string DisplayName => "クリック";
        public bool IsEnabled { get; set; } = true;

        public ClickSettings Settings { get; private set; }

        public ClickCommand(ClickSettings settings)
        {
            Settings = settings;
        }

        public async Task<ControlFlow> ExecuteAsync(IExecutionContext ctx, CancellationToken ct)
        {
            if (!IsEnabled) return ControlFlow.Next;

            try
            {
                // マウスクリックを実行
                await ExecuteMouseClickAsync(ctx, Settings.X, Settings.Y, Settings.Button, 
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
                ctx.Logger.LogError(ex, "Mouse click failed at ({X}, {Y})", Settings.X, Settings.Y);
                return ControlFlow.Error;
            }
        }

        /// <summary>
        /// マウスクリックの実行（実装はプラットフォーム固有のライブラリに依存）
        /// </summary>
        private async Task ExecuteMouseClickAsync(IExecutionContext ctx, int x, int y, MouseButton button, 
            string windowTitle, string windowClassName, CancellationToken ct)
        {
            // TODO: 実際のマウス操作実装
            // 既存のMouseHelper.Inputを使用する場合の例:
            // switch (button)
            // {
            //     case MouseButton.Left:
            //         await MouseHelper.Input.ClickAsync(x, y, windowTitle, windowClassName);
            //         break;
            //     case MouseButton.Right:
            //         await MouseHelper.Input.RightClickAsync(x, y, windowTitle, windowClassName);
            //         break;
            //     case MouseButton.Middle:
            //         await MouseHelper.Input.MiddleClickAsync(x, y, windowTitle, windowClassName);
            //         break;
            // }
            
            // 現在はシミュレーション
            await Task.Delay(10, ct); // マウスクリックの処理時間をシミュレート
            
            // ログ出力
            var buttonName = button switch
            {
                MouseButton.Left => "Left",
                MouseButton.Right => "Right", 
                MouseButton.Middle => "Middle",
                _ => button.ToString()
            };
            
            var target = string.IsNullOrEmpty(windowTitle) 
                ? $"座標 ({x}, {y})" 
                : $"ウィンドウ「{windowTitle}」の座標 ({x}, {y})";
                
            ctx.Logger.LogInformation("{Button}ボタンクリック実行: {Target}", buttonName, target);
        }

        public IEnumerable<string> Validate(IServiceProvider _)
        {
            if (Settings.X < 0)
                yield return "X座標は0以上である必要があります。";

            if (Settings.Y < 0)
                yield return "Y座標は0以上である必要があります。";

            if (Settings.X > 10000 || Settings.Y > 10000)
                yield return "座標値が異常に大きいです。画面サイズを確認してください。";

            if (!Enum.IsDefined(typeof(MouseButton), Settings.Button))
                yield return "マウスボタンの値が不正です。";
        }

        public ClickCommand DeepClone()
            => new ClickCommand(Settings with { });

        public void ReplaceSettings(ClickSettings next) => Settings = next;
    }
}