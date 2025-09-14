using AutoTool.Core.Abstractions;
using AutoTool.Core.Attributes;
using AutoTool.Core.Commands;
using AutoTool.Core.Diagnostics;
using AutoTool.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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

    public WaitImageNotExistSettings Settings { get; private set; }

    public WaitImageNotExistCommand(WaitImageNotExistSettings settings)
    {
        Settings = settings;
    }

    public async Task<ControlFlow> ExecuteAsync(CancellationToken ct)
    {
        if (!IsEnabled) return ControlFlow.Next;

        try
        {
            var timeout = Settings.TimeoutMs <= 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(Settings.TimeoutMs);
            var interval = TimeSpan.FromMilliseconds(Math.Max(Settings.IntervalMs, 50)); // 最小50ms間隔
            var sw = Stopwatch.StartNew();

            while (sw.Elapsed < timeout)
            {
                ct.ThrowIfCancellationRequested();
                // 画像が存在するかチェック
                var found = false   ;
                if (found == false)
                {
                    // 見つからなかった場合は成功
                    return ControlFlow.Next;
                }
                await Task.Delay(interval, ct);
            }

            // タイムアウトした場合は停止を返す
            return ControlFlow.Stop;
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