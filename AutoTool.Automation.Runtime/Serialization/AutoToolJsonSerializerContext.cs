using System.Text.Json.Serialization;
using AutoTool.Automation.Runtime.Lists;

namespace AutoTool.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(CommandListItem))]
[JsonSerializable(typeof(PluginCommandListItem))]
[JsonSerializable(typeof(WaitImageItem))]
[JsonSerializable(typeof(FindImageItem))]
[JsonSerializable(typeof(ClickImageItem))]
[JsonSerializable(typeof(HotkeyItem))]
[JsonSerializable(typeof(ClickItem))]
[JsonSerializable(typeof(WaitItem))]
[JsonSerializable(typeof(IfImageExistItem))]
[JsonSerializable(typeof(IfImageNotExistItem))]
[JsonSerializable(typeof(IfTextExistItem))]
[JsonSerializable(typeof(IfTextNotExistItem))]
[JsonSerializable(typeof(IfEndItem))]
[JsonSerializable(typeof(LoopItem))]
[JsonSerializable(typeof(LoopEndItem))]
[JsonSerializable(typeof(LoopBreakItem))]
[JsonSerializable(typeof(IfImageExistAIItem))]
[JsonSerializable(typeof(IfImageNotExistAIItem))]
[JsonSerializable(typeof(FindTextItem))]
[JsonSerializable(typeof(ExecuteItem))]
[JsonSerializable(typeof(SetVariableItem))]
[JsonSerializable(typeof(SetVariableAIItem))]
[JsonSerializable(typeof(SetVariableOCRItem))]
[JsonSerializable(typeof(IfVariableItem))]
[JsonSerializable(typeof(ScreenshotItem))]
[JsonSerializable(typeof(ClickImageAIItem))]
/// <summary>
/// AutoTool 用の System.Text.Json 事前生成メタデータを提供し、シリアライズ処理の性能と型安全性を高めます。
/// </summary>
internal partial class AutoToolJsonSerializerContext : JsonSerializerContext
{
}

