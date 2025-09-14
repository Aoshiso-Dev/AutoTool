using AutoTool.Commands.Input.KeyInput;
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

namespace AutoTool.Commands.Input.ClickImage
{
    /// <summary>
    /// 指定した座標でマウスクリックを実行するコマンド
    /// </summary>
    [Command("ClickImage", "クリック（画像検出）", IconKey = "mdi:cursor-default-click", Category = "マウス操作", Description = "指定した画像に対してマウスクリックを実行します", Order = 20)]
    public sealed class ClickImageCommand :
        IAutoToolCommand,
        IHasSettings<ClickImageSettings>,
        IValidatableCommand
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Type => "ClickImage";
        public string DisplayName => "クリック（画像検出）";
        public bool IsEnabled { get; set; } = true;

        private IServiceProvider? _serviceProvider = null;
        private ILogger<ClickImageCommand>? _logger = null;

        public ClickImageSettings Settings { get; private set; }

        public ClickImageCommand(ClickImageSettings settings, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = _serviceProvider.GetService(typeof(ILogger<ClickImageCommand>)) as ILogger<ClickImageCommand>;
            Settings = settings;
        }

        public async Task<ControlFlow> ExecuteAsync(CancellationToken ct)
        {
            if (!IsEnabled) return ControlFlow.Next;

            try
            {
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
                _logger?.LogError(ex, "Mouse click failed at {ImagePath}", Settings.ImagePath);
                return ControlFlow.Error;
            }
        }

        /// <summary>
        /// マウスクリックの実行（実装はプラットフォーム固有のライブラリに依存）
        /// </summary>
        private async Task ExecuteMouseClickAsync()
        {
            var ui = _serviceProvider?.GetService(typeof(IUIService)) as IUIService;
            ui?.ShowToast("ClickImageCommand");

            (int X, int Y) point = (0, 0); 

            // ログ出力
            var buttonName = Settings.Button switch
            {
                MouseButton.Left => "Left",
                MouseButton.Right => "Right", 
                MouseButton.Middle => "Middle",
                _ => Settings.Button.ToString()
            };

            var target = string.IsNullOrEmpty(Settings.WindowTitle) 
                ? $"座標 ({point.X}, {point.Y})" 
                : $"ウィンドウ「{Settings.WindowTitle}」の座標 ({point.X}, {point.Y})";
                
            _logger?.LogInformation("{Button}ボタンクリック実行: {Target}", buttonName, target);
        }

        public IEnumerable<string> Validate(IServiceProvider _)
        {
            if (string.IsNullOrWhiteSpace(Settings.ImagePath))
                yield return "画像パスが指定されていません。";
            else if (System.IO.File.Exists(Settings.ImagePath))
                yield return "画像ファイルが存在しません。";

            if (!Enum.IsDefined(typeof(MouseButton), Settings.Button))
                yield return "マウスボタンの値が不正です。";
        }
    }
}

