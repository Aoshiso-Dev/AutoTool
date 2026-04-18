using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using AutoTool.Domain.Automation.Lists;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Automation.Runtime.Definitions;
using AutoTool.Automation.Runtime.Serialization;

namespace AutoTool.Automation.Runtime.Lists;

public partial class CommandList : ObservableObject, ICommandList
{
    private readonly ICommandDefinitionProvider _commandDefinitionProvider;
    private readonly IMacroFileSerializer _macroFileSerializer;

    [ObservableProperty]
    private ObservableCollection<ICommandListItem> _items = [];

    public CommandList(ICommandDefinitionProvider commandDefinitionProvider, IMacroFileSerializer macroFileSerializer)
    {
        ArgumentNullException.ThrowIfNull(commandDefinitionProvider);
        ArgumentNullException.ThrowIfNull(macroFileSerializer);
        _commandDefinitionProvider = commandDefinitionProvider;
        _macroFileSerializer = macroFileSerializer;
    }

    public ICommandListItem this[int index]
    {
        get => Items[index];
        set => Items[index] = value;
    }

    public void Add(ICommandListItem item)
    {
        Items.Add(item);
        RebuildState();
    }

    public void Remove(ICommandListItem item)
    {
        Items.Remove(item);
        RebuildState();
    }

    /// <summary>
    /// 指定インデックスのアイテムを削除
    /// </summary>
    public void RemoveAt(int index)
    {
        if (index >= 0 && index < Items.Count)
        {
            Items.RemoveAt(index);
            RebuildState();
        }
    }

    public void Insert(int index, ICommandListItem item)
    {
        Items.Insert(index, item);
        RebuildState();
    }

    public void Override(int index, ICommandListItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if(index < 0 || index >= Items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        Items[index] = item;
        RebuildState();
    }

    public void Clear()
    {
        Items.Clear();
    }

    public void Move(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= Items.Count || newIndex < 0 || newIndex >= Items.Count)
        {
            return;
        }

        var item = Items[oldIndex];
        Items.RemoveAt(oldIndex);
        Items.Insert(newIndex, item);
        RebuildState();
    }

    public void Copy(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= Items.Count || newIndex < 0 || newIndex >= Items.Count)
        {
            return;
        }

        var item = Items[oldIndex].Clone();
        Items.Insert(newIndex, item);
        RebuildState();
    }

    public void ReorderItems()
    {
        for (var i = 0; i < Items.Count; i++)
        {
            Items[i].LineNumber = i + 1;
        }
    }

    public void CalculateNestLevel()
    {
        var nestLevel = 0;

        foreach (var item in Items)
        {
            // ネストレベルを減らすコマンド（終了系）
            if (_commandDefinitionProvider.IsEndCommand(item.ItemType))
            {
                nestLevel--;
            }

            item.NestLevel = nestLevel;

            // ネストレベルを増やすコマンド（開始系）
            if (_commandDefinitionProvider.IsStartCommand(item.ItemType))
            {
                nestLevel++;
            }
        }
    }

    public void PairIfItems()
    {
        PairItems(
            Items.OfType<IIfItem>().Where(x => _commandDefinitionProvider.IsIfCommand(x.ItemType)),
            Items.OfType<IIfEndItem>());
    }

    public void PairLoopItems()
    {
        PairItems(
            Items.OfType<ILoopItem>().Where(x => _commandDefinitionProvider.IsLoopCommand(x.ItemType)),
            Items.OfType<ILoopEndItem>());
    }

    private void RebuildState()
    {
        ReorderItems();
        CalculateNestLevel();
        PairIfItems();
        PairLoopItems();
    }

    private static void PairItems<TStart, TEnd>(IEnumerable<TStart> starts, IEnumerable<TEnd> ends)
        where TStart : class, ICommandListItem
        where TEnd : class, ICommandListItem
    {
        var orderedStarts = starts.OrderBy(x => x.LineNumber).ToList();
        var orderedEnds = ends.OrderBy(x => x.LineNumber).ToList();

        foreach (var start in orderedStarts)
        {
            SetPair(start, null);
        }

        foreach (var end in orderedEnds)
        {
            SetPair(end, null);
        }

        foreach (var start in orderedStarts)
        {
            foreach (var end in orderedEnds)
            {
                if (GetPair(end) is not null)
                {
                    continue;
                }

                if (end.NestLevel == start.NestLevel && end.LineNumber > start.LineNumber)
                {
                    SetPair(start, end);
                    SetPair(end, start);
                    break;
                }
            }
        }
    }

    private static ICommandListItem? GetPair(ICommandListItem item) => item switch
    {
        IIfItem x => x.Pair,
        IIfEndItem x => x.Pair,
        ILoopItem x => x.Pair,
        ILoopEndItem x => x.Pair,
        _ => null
    };

    private static void SetPair(ICommandListItem item, ICommandListItem? pair)
    {
        Action set = item switch
        {
            IIfItem x => () => x.Pair = pair,
            IIfEndItem x => () => x.Pair = pair,
            ILoopItem x => () => x.Pair = pair,
            ILoopEndItem x => () => x.Pair = pair,
            _ => static () => { }
        };

        set();
    }

    public IEnumerable<ICommandListItem> Clone()
    {
        List<ICommandListItem> clone = [];

        foreach (var item in Items)
        {
            clone.Add(item.Clone());
        }

        return clone;
    }

    public void Save(string filePath)
    {
        var cloneItems = Clone();

        _macroFileSerializer.SerializeToFile(cloneItems, filePath);
    }

    public void Load(string filePath)
    {
        var deserializedItems = _macroFileSerializer.DeserializeFromFile<ObservableCollection<ICommandListItem>>(filePath);
        if (deserializedItems is not null)
        {
            Items.Clear();

            foreach (var item in deserializedItems)
            {
                // 定義プロバイダーを使用して適切な型を作成
                var itemType = _commandDefinitionProvider.GetItemType(item.ItemType);
                if (itemType is not null)
                {
                    try
                    {
                        // デシリアライズされたデータから新しいアイテムを作成
                        var newItem = (ICommandListItem)Activator.CreateInstance(itemType, item)!;
                        Add(newItem);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidDataException($"型 {item.ItemType} のアイテム作成に失敗しました: {ex.Message}");
                    }
                }
                else
                {
                    throw new InvalidDataException($"不明な ItemType: {item.ItemType}");
                }
            }
        }

        CalculateNestLevel();
        PairIfItems();
        PairLoopItems();
    }
}

