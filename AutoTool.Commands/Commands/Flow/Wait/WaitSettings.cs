using AutoTool.Core.Abstractions;

namespace AutoTool.Commands.Flow.Wait
{
    /// <summary>
    /// 待機コマンドの設定
    /// </summary>
    public sealed record WaitSettings : AutoToolCommandSettings
    {
        public int Version { get; init; } = 1;
        
        /// <summary>
        /// 待機時間（ミリ秒）
        /// </summary>
        public int DurationMs { get; init; } = 1000;

        /// <summary>
        /// 待機の説明（オプション）
        /// </summary>
        public string Description { get; init; } = string.Empty;
    }
}