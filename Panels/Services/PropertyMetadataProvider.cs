using System.Reflection;
using MacroPanels.Attributes;
using MacroPanels.Model.List.Interface;

namespace MacroPanels.Services;

/// <summary>
/// CommandListItemからプロパティメタデータを取得するサービス
/// </summary>
public class PropertyMetadataProvider
{
    /// <summary>
    /// アイテムからメタデータ付きプロパティを取得
    /// </summary>
    public IEnumerable<PropertyMetadata> GetMetadata(ICommandListItem? item)
    {
        if (item == null) yield break;
        
        var properties = item.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<CommandPropertyAttribute>() != null)
            .Select(p => new PropertyMetadata
            {
                PropertyInfo = p,
                Attribute = p.GetCustomAttribute<CommandPropertyAttribute>()!,
                Target = item
            })
            .OrderBy(m => m.Group)
            .ThenBy(m => m.Order);
        
        foreach (var metadata in properties)
        {
            yield return metadata;
        }
    }
    
    /// <summary>
    /// アイテムからグループ化されたメタデータを取得
    /// </summary>
    public IEnumerable<PropertyGroup> GetGroupedMetadata(ICommandListItem? item)
    {
        if (item == null) yield break;
        
        var groups = GetMetadata(item)
            .GroupBy(m => m.Group)
            .OrderBy(g => g.Min(m => m.Order))
            .Select(g => new PropertyGroup
            {
                GroupName = g.Key,
                Properties = g.OrderBy(m => m.Order).ToList()
            });
        
        foreach (var group in groups)
        {
            yield return group;
        }
    }
    
    /// <summary>
    /// アイテムが編集可能なプロパティを持つかどうか
    /// </summary>
    public bool HasEditableProperties(ICommandListItem? item)
    {
        if (item == null) return false;
        
        return item.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(p => p.GetCustomAttribute<CommandPropertyAttribute>() != null);
    }
}
