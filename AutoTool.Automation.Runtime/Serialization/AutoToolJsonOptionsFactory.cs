using System.Text.Json;

namespace AutoTool.Serialization;

/// <summary>
/// 利用目的に応じたインスタンスや設定済みオブジェクトを生成します。
/// </summary>
public static class AutoToolJsonOptionsFactory
{
    public static JsonSerializerOptions CreateMacroSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        options.TypeInfoResolverChain.Insert(0, new CommandListItemPolymorphicResolver());
        return options;
    }

}
