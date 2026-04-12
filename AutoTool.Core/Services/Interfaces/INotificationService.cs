// AutoTool固有の通知サービスインターフェースは廃止
// AutoTool.Commands.Services.INotificationService を使用してください
// このファイルは互換性のために残しています

namespace AutoTool.Services.Interfaces;

/// <summary>
/// 通知サービスのインターフェース
/// </summary>
/// <remarks>
/// このインターフェースは非推奨です。
/// <see cref="AutoTool.Commands.Services.INotificationService"/> を使用してください。
/// </remarks>
[Obsolete("AutoTool.Commands.Services.INotificationService を使用してください")]
public interface INotificationService : AutoTool.Commands.Services.INotificationService
{
}

