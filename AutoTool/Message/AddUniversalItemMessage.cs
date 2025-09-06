using AutoTool.ViewModel.Shared;

namespace AutoTool.Message
{
    /// <summary>
    /// 動的コマンド追加メッセージ
    /// </summary>
    /// <param name="Item">追加するUniversalCommandItem</param>
    public record AddUniversalItemMessage(UniversalCommandItem Item);
}