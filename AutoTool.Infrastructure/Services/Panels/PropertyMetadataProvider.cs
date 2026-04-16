using System.Reflection;
using AutoTool.Panels.Attributes;
using AutoTool.Panels.Model.List.Interface;

namespace AutoTool.Panels.Services;

/// <summary>
/// CommandListItem����v���p�e�B���^�f�[�^��擾����T�[�r�X
/// </summary>
public class PropertyMetadataProvider
{
    /// <summary>
    /// �A�C�e�����烁�^�f�[�^�t���v���p�e�B��擾
    /// </summary>
    public IEnumerable<PropertyMetadata> GetMetadata(ICommandListItem? item)
    {
        if (item is null) yield break;
        
        var properties = item.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<CommandPropertyAttribute>() is not null)
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
    /// �A�C�e������O���[�v�����ꂽ���^�f�[�^��擾
    /// </summary>
    public IEnumerable<PropertyGroup> GetGroupedMetadata(ICommandListItem? item)
    {
        if (item is null) yield break;
        
        var groups = GetMetadata(item)
            .GroupBy(m => m.Group)
            .OrderBy(g => g.Min(m => m.Order))
            .Select(g => new PropertyGroup
            {
                GroupName = g.Key,
                Properties = [.. g.OrderBy(m => m.Order)]
            });
        
        foreach (var group in groups)
        {
            yield return group;
        }
    }
    
    /// <summary>
    /// �A�C�e�����ҏW�\�ȃv���p�e�B������ǂ���
    /// </summary>
    public bool HasEditableProperties(ICommandListItem? item)
    {
        if (item is null) return false;
        
        return item.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(p => p.GetCustomAttribute<CommandPropertyAttribute>() is not null);
    }
}

