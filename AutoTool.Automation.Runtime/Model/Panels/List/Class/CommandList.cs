using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Automation.Runtime.Definitions;

namespace AutoTool.Automation.Runtime.Lists;

public partial class CommandList : ObservableObject
{
    private readonly ICommandDefinitionProvider _commandDefinitionProvider;

    [ObservableProperty]
    private ObservableCollection<ICommandListItem> _items = [];

    public CommandList(ICommandDefinitionProvider commandDefinitionProvider)
    {
        ArgumentNullException.ThrowIfNull(commandDefinitionProvider);
        _commandDefinitionProvider = commandDefinitionProvider;
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

    public void PairRetryItems()
    {
        PairItems(
            Items.OfType<IRetryItem>(),
            Items.OfType<IRetryEndItem>());
    }

    private void RebuildState()
    {
        ReorderItems();
        CalculateNestLevel();
        PairIfItems();
        PairLoopItems();
        PairRetryItems();
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
        IRetryItem x => x.Pair,
        IRetryEndItem x => x.Pair,
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
            IRetryItem x => () => x.Pair = pair,
            IRetryEndItem x => () => x.Pair = pair,
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

}

