using System.Diagnostics;

namespace MacroPanels.Command.Services;

/// <summary>
/// プロセス実行サービスのインターフェース
/// </summary>
public interface IProcessService
{
    /// <summary>
    /// プログラムを実行します
    /// </summary>
    /// <param name="programPath">プログラムのパス</param>
    /// <param name="arguments">引数</param>
    /// <param name="workingDirectory">作業ディレクトリ</param>
    Task StartAsync(string programPath, string? arguments = null, string? workingDirectory = null);

    /// <summary>
    /// プログラムを実行します（終了待機オプション付き）
    /// </summary>
    /// <param name="programPath">プログラムのパス</param>
    /// <param name="arguments">引数</param>
    /// <param name="workingDirectory">作業ディレクトリ</param>
    /// <param name="waitForExit">終了を待つかどうか</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task StartAsync(string programPath, string? arguments, string? workingDirectory, bool waitForExit, CancellationToken cancellationToken = default);
}
