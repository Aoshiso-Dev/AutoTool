using MacroPanels.Command.Interface;
using MacroPanels.Command.Class;
using MacroPanels.List.Class;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MacroPanels.Model.List.Interface;
using MacroPanels.Model.CommandDefinition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MacroPanels.Model.MacroFactory
{
    /// <summary>
    /// マクロファクトリ（DI,Plugin統合版）
    /// </summary>
    public static class MacroFactory
    {
        /// <summary>
        /// コマンドリストからマクロを作成
        /// </summary>
        public static ICommand CreateMacro(IEnumerable<ICommandListItem> items)
        {
            var cloneItems = items.Select(x => x.Clone()).ToList();
            var root = new LoopCommand(new RootCommand(), new LoopCommandSettings() { LoopCount = 1 });
            
            // 子コマンドを追加
            var childCommands = ListItemToCommand(root, cloneItems);
            foreach (var child in childCommands)
            {
                root.AddChild(child);
            }
            
            return root;
        }

        /// <summary>
        /// リストアイテムをコマンドに変換
        /// </summary>
        private static IEnumerable<ICommand> ListItemToCommand(ICommand parent, IList<ICommandListItem> listItems)
        {
            var commands = new List<ICommand>();

            for (int i = 0; i < listItems.Count; i++)
            {
                if (listItems[i].IsEnable == false)
                    continue;

                var listItem = listItems[i];
                var command = ItemToCommand(parent, listItem, listItems, ref i);
                
                if (command != null)
                {
                    commands.Add(command);
                }
            }

            return commands;
        }

        /// <summary>
        /// アイテムをコマンドに変換
        /// </summary>
        private static ICommand? ItemToCommand(ICommand parent, ICommandListItem listItem, IList<ICommandListItem> listItems, ref int index)
        {
            ICommand? command = null;

            switch (listItem)
            {
                case WaitImageItem waitImageItem:
                    command = new WaitImageCommand(parent, waitImageItem);
                    break;

                case ClickImageItem clickImageItem:
                    command = new ClickImageCommand(parent, clickImageItem);
                    break;

                case ClickImageAIItem clickImageAIItem:
                    command = new ClickImageAICommand(parent, clickImageAIItem);
                    break;

                case HotkeyItem hotkeyItem:
                    command = new HotkeyCommand(parent, hotkeyItem);
                    break;

                case ClickItem clickItem:
                    command = new ClickCommand(parent, clickItem);
                    break;

                case WaitItem waitItem:
                    command = new WaitCommand(parent, waitItem);
                    break;

                case LoopItem loopItem:
                    {
                        var loopCommand = new LoopCommand(parent, loopItem);

                        if (loopItem.Pair == null)
                            throw new InvalidOperationException($"Loop (行 {loopItem.LineNumber}) に対応するEndLoopがありません");

                        var childrenListItems = GetChildrenListItems(listItem, listItems);

                        if (childrenListItems.Count == 0)
                            throw new InvalidOperationException($"Loop (行 {loopItem.LineNumber}) 内にコマンドがありません");

                        // 子コマンドを追加
                        var childCommands = ListItemToCommand(loopCommand, childrenListItems);
                        foreach (var child in childCommands)
                        {
                            loopCommand.AddChild(child);
                        }

                        // LoopEndまでのインデックスを進める
                        index = GetItemIndex(listItems, loopItem.Pair);

                        command = loopCommand;
                    }
                    break;

                case LoopBreakItem loopBreakItem:
                    command = new LoopBreakCommand(parent, loopBreakItem);
                    break;

                case LoopEndItem:
                    // EndLoopは親のLoopで処理されているのでnull
                    break;

                case IfImageExistItem ifImageExistItem:
                    {
                        var ifCommand = CreateIfCommandInstance(parent, ifImageExistItem);
                        
                        if (ifImageExistItem.Pair == null)
                            throw new InvalidOperationException($"IfImageExist (行 {ifImageExistItem.LineNumber}) に対応するEndIfがありません");

                        var childrenListItems = GetChildrenListItems(listItem, listItems);

                        // 子コマンドを追加
                        var childCommands = ListItemToCommand(ifCommand, childrenListItems);
                        foreach (var child in childCommands)
                        {
                            ifCommand.AddChild(child);
                        }

                        // EndIfまでのインデックスを進める
                        index = GetItemIndex(listItems, ifImageExistItem.Pair);

                        command = ifCommand;
                    }
                    break;

                case IfImageNotExistItem ifImageNotExistItem:
                    {
                        var ifCommand = CreateIfCommandInstance(parent, ifImageNotExistItem);
                        
                        if (ifImageNotExistItem.Pair == null)
                            throw new InvalidOperationException($"IfImageNotExist (行 {ifImageNotExistItem.LineNumber}) に対応するEndIfがありません");

                        var childrenListItems = GetChildrenListItems(listItem, listItems);

                        // 子コマンドを追加
                        var childCommands = ListItemToCommand(ifCommand, childrenListItems);
                        foreach (var child in childCommands)
                        {
                            ifCommand.AddChild(child);
                        }

                        // EndIfまでのインデックスを進める
                        index = GetItemIndex(listItems, ifImageNotExistItem.Pair);

                        command = ifCommand;
                    }
                    break;

                case IfImageExistAIItem ifImageExistAIItem:
                    {
                        var ifCommand = new IfImageExistAICommand(parent, ifImageExistAIItem);
                        
                        if (ifImageExistAIItem.Pair == null)
                            throw new InvalidOperationException($"IfImageExistAI (行 {ifImageExistAIItem.LineNumber}) に対応するEndIfがありません");

                        var childrenListItems = GetChildrenListItems(listItem, listItems);

                        // 子コマンドを追加
                        var childCommands = ListItemToCommand(ifCommand, childrenListItems);
                        foreach (var child in childCommands)
                        {
                            ifCommand.AddChild(child);
                        }

                        // EndIfまでのインデックスを進める
                        index = GetItemIndex(listItems, ifImageExistAIItem.Pair);

                        command = ifCommand;
                    }
                    break;

                case IfImageNotExistAIItem ifImageNotExistAIItem:
                    {
                        var ifCommand = new IfImageNotExistAICommand(parent, ifImageNotExistAIItem);
                        
                        if (ifImageNotExistAIItem.Pair == null)
                            throw new InvalidOperationException($"IfImageNotExistAI (行 {ifImageNotExistAIItem.LineNumber}) に対応するEndIfがありません");

                        var childrenListItems = GetChildrenListItems(listItem, listItems);

                        // 子コマンドを追加
                        var childCommands = ListItemToCommand(ifCommand, childrenListItems);
                        foreach (var child in childCommands)
                        {
                            ifCommand.AddChild(child);
                        }

                        // EndIfまでのインデックスを進める
                        index = GetItemIndex(listItems, ifImageNotExistAIItem.Pair);

                        command = ifCommand;
                    }
                    break;

                case IfVariableItem ifVariableItem:
                    {
                        var ifCommand = new IfVariableCommand(parent, ifVariableItem);
                        
                        if (ifVariableItem.Pair == null)
                            throw new InvalidOperationException($"IfVariable (行 {ifVariableItem.LineNumber}) に対応するEndIfがありません");

                        var childrenListItems = GetChildrenListItems(listItem, listItems);

                        // 子コマンドを追加
                        var childCommands = ListItemToCommand(ifCommand, childrenListItems);
                        foreach (var child in childCommands)
                        {
                            ifCommand.AddChild(child);
                        }

                        // EndIfまでのインデックスを進める
                        index = GetItemIndex(listItems, ifVariableItem.Pair);

                        command = ifCommand;
                    }
                    break;

                case IfEndItem:
                    // EndIfは親のIfで処理されているのでnull
                    break;

                case ExecuteItem executeItem:
                    command = new ExecuteCommand(parent, executeItem);
                    break;

                case SetVariableItem setVariableItem:
                    command = new SetVariableCommand(parent, setVariableItem);
                    break;

                case SetVariableAIItem setVariableAIItem:
                    command = new SetVariableAICommand(parent, setVariableAIItem);
                    break;

                case ScreenshotItem screenshotItem:
                    command = new ScreenshotCommand(parent, screenshotItem);
                    break;
            }

            return command;
        }

        /// <summary>
        /// If系コマンドのインスタンスを作成
        /// </summary>
        private static ICommand CreateIfCommandInstance(ICommand parent, ICommandListItem listItem)
        {
            return listItem switch
            {
                IfImageExistItem ifImageExistItem => new IfImageExistCommand(parent, ifImageExistItem),
                IfImageNotExistItem ifImageNotExistItem => new IfImageNotExistCommand(parent, ifImageNotExistItem),
                _ => throw new NotSupportedException($"未対応のIfアイテム: {listItem.GetType().Name}")
            };
        }

        /// <summary>
        /// 子アイテムのリストを取得
        /// </summary>
        private static IList<ICommandListItem> GetChildrenListItems(ICommandListItem listItem, IList<ICommandListItem> listItems)
        {
            var childrenListItems = new List<ICommandListItem>();

            ICommandListItem? endItem = null;
            
            switch (listItem)
            {
                case ILoopItem loopItem:
                    endItem = loopItem.Pair;
                    break;
                case IIfItem ifItem:
                    endItem = ifItem.Pair;
                    break;
            }

            if (endItem == null) return childrenListItems;

            for (int i = 0; i < listItems.Count; i++)
            {
                if (listItems[i].LineNumber > listItem.LineNumber && listItems[i].LineNumber < endItem.LineNumber)
                {
                    childrenListItems.Add(listItems[i]);
                }
            }

            return childrenListItems;
        }

        /// <summary>
        /// アイテムのインデックスを取得
        /// </summary>
        private static int GetItemIndex(IList<ICommandListItem> listItems, ICommandListItem targetItem)
        {
            for (int i = 0; i < listItems.Count; i++)
            {
                if (listItems[i] == targetItem)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
