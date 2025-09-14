using AutoTool.Commands.Input.ClickImage;
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

namespace AutoTool.Commands.Input.Click
{
    /// <summary>
    /// 指定した座標でマウスクリックを実行するコマンド
    /// </summary>
    [Command("Click", "クリック", IconKey = "mdi:cursor-default-click", Category = "マウス操作", Description = "指定した座標でマウスクリックを実行します", Order = 20)]
    public sealed class ClickCommand :
        IAutoToolCommand,
        IHasSettings<ClickSettings>,
        IValidatableCommand
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Type => "Click";
        public string DisplayName => "クリック";
        public bool IsEnabled { get; set; } = true;

        private IServiceProvider? _serviceProvider = null;
        private ILogger<ClickCommand>? _logger = null;

        public ClickSettings Settings { get; private set; }

        public ClickCommand(ClickSettings settings, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = _serviceProvider.GetService(typeof(ILogger<ClickCommand>)) as ILogger<ClickCommand>;
            Settings = settings;
        }

        public async Task<ControlFlow> ExecuteAsync(CancellationToken ct)
        {
            if (!IsEnabled) return ControlFlow.Next;

            try
            {
                // マウスクリックを実行
                await ExecuteMouseClickAsync();

                return ControlFlow.Next;
            }
            catch (OperationCanceledException)
            {
                // キャンセル時は停止を返す
                return ControlFlow.Stop;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Mouse click failed at ({X}, {Y})", Settings.Point.X, Settings.Point.Y);
                return ControlFlow.Error;
            }
        }

        /// <summary>
        /// マウスクリックの実行（実装はプラットフォーム固有のライブラリに依存）
        /// </summary>
        private async Task ExecuteMouseClickAsync()
        {
            var ui = _serviceProvider?.GetService(typeof(IUIService)) as IUIService;
            ui?.ShowToast("ClickCommand");

            // ログ出力
            var buttonName = Settings.Button switch
            {
                MouseButton.Left => "Left",
                MouseButton.Right => "Right", 
                MouseButton.Middle => "Middle",
                _ => Settings.Button.ToString()
            };
            
            var target = string.IsNullOrEmpty(Settings.WindowTitle) 
                ? $"座標 ({Settings.Point.X}, {Settings.Point.Y})" 
                : $"ウィンドウ「{Settings.WindowTitle}」の座標 ({Settings.Point.X}, {Settings.Point.Y})";
                
            _logger?.LogInformation("{Button}ボタンクリック実行: {Target}", buttonName, target);
        }

        public IEnumerable<string> Validate(IServiceProvider _)
        {
            if (Settings.Point.X < 0)
                yield return "X座標は0以上である必要があります。";

            if (Settings.Point.Y < 0)
                yield return "Y座標は0以上である必要があります。";

            if (Settings.Point.X > 10000 || Settings.Point.Y > 10000)
                yield return "座標値が異常に大きいです。画面サイズを確認してください。";

            if (!Enum.IsDefined(typeof(MouseButton), Settings.Button))
                yield return "マウスボタンの値が不正です。";
        }
    }
}