using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using AutoTool.Automation.Runtime.Definitions;
using AutoTool.Automation.Contracts.Lists;

namespace AutoTool.Serialization;

internal sealed class CommandListItemPolymorphicResolver : IJsonTypeInfoResolver
{
    private readonly IJsonTypeInfoResolver _innerResolver = new DefaultJsonTypeInfoResolver();

    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var typeInfo = _innerResolver.GetTypeInfo(type, options);
        if (typeInfo is null || type != typeof(ICommandListItem))
        {
            return typeInfo;
        }

        var polymorphism = new JsonPolymorphismOptions
        {
            TypeDiscriminatorPropertyName = nameof(ICommandListItem.ItemType),
            IgnoreUnrecognizedTypeDiscriminators = false,
            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization
        };

        foreach (var metadata in CommandMetadataCatalog.GetAll())
        {
            polymorphism.DerivedTypes.Add(new JsonDerivedType(metadata.ItemType, metadata.TypeName));
        }

        typeInfo.PolymorphismOptions = polymorphism;
        return typeInfo;
    }
}
