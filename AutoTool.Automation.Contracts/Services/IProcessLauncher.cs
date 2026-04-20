namespace AutoTool.Commands.Services;

/// <summary>
/// 関連データを不変に保持するレコード型です。
/// </summary>
public sealed record ProcessOutputLine(string Text, bool IsError, DateTimeOffset Timestamp);

/// <summary>
/// プロセス起動のインターフェース
/// </summary>
public interface IProcessLauncher
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

    /// <summary>
    /// 標準出力/標準エラーをストリームとして取得しながらプログラムを実行します。
    /// </summary>
    IAsyncEnumerable<ProcessOutputLine> StartWithOutputAsync(
        string programPath,
        string? arguments = null,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default);
}
