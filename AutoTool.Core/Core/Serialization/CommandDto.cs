using System.Text.Json;


namespace AutoTool.Core.Serialization;


public sealed class CommandDto
{
    public Guid Id { get; set; }
    public required string Type { get; set; }
    public bool IsEnabled { get; set; } = true;


    // 設定は型非依存で保持（Descriptorの SettingsType でデシリアライズ）
    public required JsonElement Settings { get; set; }


    // ブロック名 -> 子DTO配列（Then/Else/Bodyなど）
    public Dictionary<string, List<CommandDto>>? Blocks { get; set; }
}