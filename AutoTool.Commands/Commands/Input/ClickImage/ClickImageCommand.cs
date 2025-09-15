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
using System.Diagnostics;
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
        private IImageService? _imageService = null;
        private IMouseService? _mouseService = null;

        public ClickImageSettings Settings { get; private set; }

        public ClickImageCommand(ClickImageSettings settings, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = _serviceProvider.GetService(typeof(ILogger<ClickImageCommand>)) as ILogger<ClickImageCommand> ?? throw new ArgumentNullException(nameof(ILogger));
            _imageService = _serviceProvider.GetService(typeof(IImageService)) as IImageService ?? throw new ArgumentNullException(nameof(IImageService));
            _mouseService = _serviceProvider.GetService(typeof(IMouseService)) as IMouseService ?? throw new ArgumentNullException(nameof(IMouseService));
            Settings = settings;
        }

        public async Task<ControlFlow> ExecuteAsync(CancellationToken ct)
        {
            if (!IsEnabled) return ControlFlow.Next;

            try
            {
                var sw = Stopwatch.StartNew();
                var timeout = TimeSpan.FromMilliseconds(Settings.TimeoutMs);
                var interval = TimeSpan.FromMilliseconds(Math.Max(Settings.IntervalMs, 10));

                Func<string, double, CancellationToken, Task<Point?>> func =
                    (Settings.WindowTitle == string.Empty && Settings.WindowClassName == string.Empty)
                    ? _imageService!.SearchImageOnScreenAsync
                    : (imagePath, threshold, cancellationToken) => _imageService!.SearchImageInWindowAsync(imagePath, Settings.WindowTitle, Settings.WindowClassName, threshold, cancellationToken);


                Point? result = null;

                while (true)
                {
                    // タイムアウトチェック
                    if (sw.Elapsed > timeout)
                    {
                        _logger?.LogWarning("画像が見つかりませんでした: {ImagePath}", Settings.ImagePath);
                        return ControlFlow.Error;
                    }

                    // 画像から座標を取得
                    result = await func(Settings.ImagePath, Settings.Similarity, ct);

                    if (result.HasValue)
                    {
                        // 座標が見つかったらループを抜ける
                        break;
                    }

                    // 少し待ってから再試行
                    await Task.Delay(interval, ct);
                }

                // マウスクリックを実行
                switch (Settings.Button)
                {
                    case MouseButton.Left:
                        await _mouseService!.ClickAsync((int)result.Value.X, (int)result.Value.Y, Settings.WindowTitle, Settings.WindowClassName);
                        break;
                    case MouseButton.Right:
                        await _mouseService!.RightClickAsync((int)result.Value.X, (int)result.Value.Y, Settings.WindowTitle, Settings.WindowClassName);
                        break;
                    case MouseButton.Middle:
                        await _mouseService!.MiddleClickAsync((int)result.Value.X, (int)result.Value.Y, Settings.WindowTitle, Settings.WindowClassName);
                        break;
                    default:
                        throw new InvalidOperationException("Unsupported mouse button: " + Settings.Button);
                }

                // ログ出力
                var buttonName = Settings.Button switch
                {
                    MouseButton.Left => "Left",
                    MouseButton.Right => "Right",
                    MouseButton.Middle => "Middle",
                    _ => Settings.Button.ToString()
                };

                var target = string.IsNullOrEmpty(Settings.WindowTitle)
                    ? $"座標 ({result.Value.X}, {result.Value.Y})"
                    : $"ウィンドウ「{Settings.WindowTitle}」の座標 ({result.Value.X}, {result.Value.Y})";

                _logger?.LogInformation("{Button}ボタンクリック実行: {Target}", buttonName, target);
                
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

