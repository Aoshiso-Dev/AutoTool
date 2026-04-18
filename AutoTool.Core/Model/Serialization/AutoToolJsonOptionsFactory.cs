using System.Text.Json;

namespace AutoTool.Serialization;

public static class AutoToolJsonOptionsFactory
{
    public static JsonSerializerOptions CreateMacroSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        options.TypeInfoResolverChain.Insert(0, new CommandListItemPolymorphicResolver());
        options.TypeInfoResolverChain.Insert(0, AutoToolJsonSerializerContext.Default);
        return options;
    }

}
