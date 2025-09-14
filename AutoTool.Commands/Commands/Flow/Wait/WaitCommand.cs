using AutoTool.Core.Abstractions;
using AutoTool.Core.Attributes;
using AutoTool.Core.Commands;
using AutoTool.Core.Diagnostics;
using AutoTool.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTool.Commands.Flow.Wait;

/// <summary>
/// 指定した時間だけ待機するコマンド
/// </summary>
[Command("Wait", "待機", IconKey = "mdi:timer", Category = "フロー制御", Description = "指定した時間だけ処理を待機します", Order = 10)]
public sealed class WaitCommand :
    IAutoToolCommand,
    IHasSettings<WaitSettings>,
    IValidatableCommand
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Type => "Wait";
    public string DisplayName => "待機";
    public bool IsEnabled { get; set; } = true;

    public WaitSettings Settings { get; private set; }

    public WaitCommand(WaitSettings settings)
    {
        Settings = settings;
    }

    public async Task<ControlFlow> ExecuteAsync(CancellationToken ct)
    {
        if (!IsEnabled) return ControlFlow.Next;

        try
        {
            // DelayAsyncを使って指定時間待機
            var duration = TimeSpan.FromMilliseconds(Settings.DurationMs);
            await Task.Delay(duration, ct);

            return ControlFlow.Next;
        }
        catch (OperationCanceledException)
        {
            // キャンセル時は停止を返す
            return ControlFlow.Stop;
        }
    }

    public IEnumerable<string> Validate(IServiceProvider _)
    {
        if (Settings.DurationMs < 0)
            yield return "待機時間は0以上である必要があります。";

        if (Settings.DurationMs > 300000) // 5分以上の場合は警告
            yield return "待機時間が5分を超えています。意図した値か確認してください。";
    }
}