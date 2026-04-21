using System.Text.Json;

namespace AutoTool.Serialization;

/// <summary>
/// 利用目的に応じたインスタンスや設定済みオブジェクトを生成します。
/// </summary>
public static class AutoToolJsonOptionsFactory
{
    public static JsonSerializerOptions CreateMacroSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }

}
