using AutoTool.Core.Abstractions;
using AutoTool.Core.Attributes;
using AutoTool.Core.Commands;
using AutoTool.Core.Diagnostics;
using AutoTool.Core.Utilities;
using AutoTool.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace AutoTool.Commands.Flow.Wait;

/// <summary>
/// 画像が存在するようになるまで待機するコマンド
/// </summary>
[Command("WaitImageNotExist", "待機（画像非存在）", IconKey = "mdi:timer", Category = "フロー制御", Description = "指定した画像が存在しなくなるまで待機します", Order = 10)]
public sealed class WaitImageNotExistCommand :
    IAutoToolCommand,
    IHasSettings<WaitImageNotExistSettings>,
    IValidatableCommand
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Type => "WaitImageNotExist";
    public string DisplayName => "待機（画像非存在）";
    public bool IsEnabled { get; set; } = true;

    private IServiceProvider? _serviceProvider = null;
    private ILogger<WaitImageExistCommand>? _logger = null;
    private IImageService? _imageService = null;

    public WaitImageNotExistSettings Settings { get; private set; }

    public WaitImageNotExistCommand(WaitImageNotExistSettings settings, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = _serviceProvider.GetService(typeof(ILogger<WaitImageExistCommand>)) as ILogger<WaitImageExistCommand> ?? throw new ArgumentNullException(nameof(ILogger));
        _imageService = _serviceProvider.GetService(typeof(IImageService)) as IImageService ?? throw new ArgumentNullException(nameof(IImageService));
        Settings = settings;
    }

    public async Task<ControlFlow> ExecuteAsync(CancellationToken ct)
    {
        if (!IsEnabled) return ControlFlow.Next;

        try
        {
            var timeout = Settings.TimeoutMs <= 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(Settings.TimeoutMs);
            var interval = TimeSpan.FromMilliseconds(Math.Max(Settings.IntervalMs, 10)); // 最小10ms間隔
            var sw = Stopwatch.StartNew();

            Func<string, double, CancellationToken, Task<Point?>> func =
                (Settings.WindowTitle == string.Empty && Settings.WindowClassName == string.Empty)
                ? _imageService!.SearchImageOnScreenAsync
                : (imagePath, threshold, cancellationToken) => _imageService!.SearchImageInWindowAsync(imagePath, Settings.WindowTitle, Settings.WindowClassName, threshold, cancellationToken);

            while (sw.Elapsed < timeout)
            {
                ct.ThrowIfCancellationRequested();
                // 画像が存在するかチェック

                var result = await func(Settings.ImagePath, Settings.Similarity, ct);

                if (result.HasValue == false)
                {
                    // 画像が見つからなかった場合は次へ進む
                    return ControlFlow.Next;
                }

                await Task.Delay(interval, ct);
            }

            // タイムアウトした場合はエラーを返す
            return ControlFlow.Error;
        }
        catch (OperationCanceledException)
        {
            // キャンセル時は停止を返す
            return ControlFlow.Stop;
        }
    }

    public IEnumerable<string> Validate(IServiceProvider _)
    {
        if (string.IsNullOrWhiteSpace(Settings.ImagePath))
            yield return "画像パスが指定されていません。";
        else if(System.IO.Path.Exists(Settings.ImagePath) == false)
            yield return "指定された画像パスが存在しません。";
        if (Settings.Similarity < 0.0 || Settings.Similarity > 1.0)
            yield return "類似度は0.0から1.0の範囲で指定してください。";
        if (Settings.TimeoutMs < 0)
            yield return "タイムアウトは0以上の値で指定してください。";
        if (Settings.IntervalMs < 50)
            yield return "チェック間隔は50ms以上の値で指定してください。";
    }
}