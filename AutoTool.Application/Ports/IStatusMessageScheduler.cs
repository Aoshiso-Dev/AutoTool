using System;

namespace AutoTool.Application.Ports;

/// <summary>
/// ステータスメッセージ表示の遅延実行を提供するポートです。
/// </summary>
public interface IStatusMessageScheduler
{
    /// <summary>指定遅延後にアクションを実行します。</summary>
    void Schedule(TimeSpan delay, Action action);
}

