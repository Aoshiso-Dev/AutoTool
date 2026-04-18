using System.Text.Json.Serialization;
using AutoTool.Panels.List.Class;

namespace AutoTool.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(CommandListItem))]
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
internal partial class AutoToolJsonSerializerContext : JsonSerializerContext
{
}
