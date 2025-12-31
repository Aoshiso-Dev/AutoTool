// AutoTool固有の通知サービスインターフェースは廃止
// MacroPanels.Command.Services.INotificationService を使用してください
// このファイルは互換性のために残しています

namespace AutoTool.Services.Interfaces;

/// <summary>
/// 通知サービスのインターフェース
/// </summary>
/// <remarks>
/// このインターフェースは非推奨です。
/// <see cref="MacroPanels.Command.Services.INotificationService"/> を使用してください。
/// </remarks>
[Obsolete("MacroPanels.Command.Services.INotificationService を使用してください")]
public interface INotificationService : MacroPanels.Command.Services.INotificationService
{
}
