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
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MacroPanels.Plugin;

namespace MacroPanels.Model.MacroFactory
{
    /// <summary>
    /// マクロファクトリ（DI,Plugin統合版）
    /// </summary>
    public static class MacroFactory
    {
        private static IServiceProvider? _serviceProvider;
        private static IPluginService? _pluginService;
        private static ILogger? _logger;
        private static int _buildSessionId = 0;
        private const int MaxRecursionDepth = 100;

        /// <summary>
        /// サービスプロバイダーを設定
        /// </summary>
        public static void SetServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("MacroFactory");
        }

        /// <summary>
        /// プラグインサービスを設定
        /// </summary>
        public static void SetPluginService(IPluginService pluginService)
        {
            _pluginService = pluginService;
        }

        /// <summary>
        /// コマンドリストからマクロを作成
        /// </summary>
        public static ICommand CreateMacro(IEnumerable<ICommandListItem> items)
        {
            var sessionId = Interlocked.Increment(ref _buildSessionId);
            var swTotal = Stopwatch.StartNew();
            try
            {
                var originalList = items.ToList();
                var cloneSw = Stopwatch.StartNew();
                var cloneItems = originalList.Select(x => x.Clone()).ToList();
                cloneSw.Stop();
                _logger?.LogDebug("[MacroFactory:{Session}] CreateMacro start Items={Count} CloneElapsed={CloneMs}ms", sessionId, cloneItems.Count, cloneSw.ElapsedMilliseconds);

                // Pair再構築 (Cloneで元オブジェクトの参照をコピーしてしまいインデックス探索が失敗する問題対策)
                var pairFixSw = Stopwatch.StartNew();
                RebuildPairRelationships(cloneItems, originalList, sessionId);
                pairFixSw.Stop();
                _logger?.LogDebug("[MacroFactory:{Session}] Pair rebuild elapsed={Ms}ms", sessionId, pairFixSw.ElapsedMilliseconds);

                var root = new LoopCommand(new RootCommand(_serviceProvider), new LoopCommandSettings() { LoopCount = 1 }, _serviceProvider)
                {
                    LineNumber = 0
                };

                var childSw = Stopwatch.StartNew();
                var childCommands = ListItemToCommand(root, cloneItems, 0, sessionId);
                foreach (var child in childCommands) root.AddChild(child);
                childSw.Stop();
                swTotal.Stop();
                _logger?.LogDebug("[MacroFactory:{Session}] CreateMacro complete Children={ChildCount} TotalElapsed={TotalMs}ms BuildElapsed={BuildMs}ms", sessionId, root.Children.Count(), swTotal.ElapsedMilliseconds, childSw.ElapsedMilliseconds);
                return root;
            }
            catch (Exception ex)
            {
                swTotal.Stop();
                _logger?.LogError(ex, "[MacroFactory:{Session}] CreateMacro failed after {Ms}ms", sessionId, swTotal.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Clone後に失われた Pair 関係を LineNumber ベースで再構築
        /// </summary>
        private static void RebuildPairRelationships(IList<ICommandListItem> cloned, IList<ICommandListItem> original, int sessionId)
        {
            try
            {
                var lineToClone = cloned.GroupBy(c => c.LineNumber).ToDictionary(g => g.Key, g => g.First());
                var lineToOrig = original.GroupBy(o => o.LineNumber).ToDictionary(g => g.Key, g => g.First());
                int fixedCount = 0, missingPair = 0, unresolved = 0;
                foreach (var clone in cloned)
                {
                    var pairProp = clone.GetType().GetProperty("Pair");
                    if (pairProp == null) continue; // 対象外

                    // 元アイテム取得
                    if (!lineToOrig.TryGetValue(clone.LineNumber, out var origItem))
                    {
                        unresolved++;
                        continue;
                    }
                    var origPair = pairProp.GetValue(origItem) as ICommandListItem;
                    if (origPair == null)
                    {
                        missingPair++;
                        continue; // Pair無し
                    }
                    if (!lineToClone.TryGetValue(origPair.LineNumber, out var clonePair))
                    {
                        unresolved++;
                        continue; // 対応クローン未発見
                    }
                    // 既に正しいならスキップ
                    var currentPair = pairProp.GetValue(clone) as ICommandListItem;
                    if (currentPair != clonePair)
                    {
                        pairProp.SetValue(clone, clonePair);
                        fixedCount++;
                    }
                }
                _logger?.LogDebug("[MacroFactory:{Session}] Pair rebuild result Fixed={Fixed} MissingOrigPair={Missing} Unresolved={Unresolved}", sessionId, fixedCount, missingPair, unresolved);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[MacroFactory:{Session}] Pair rebuild でエラー", sessionId);
            }
        }

        /// <summary>
        /// リストアイテムをコマンドに変換
        /// </summary>
        private static IEnumerable<ICommand> ListItemToCommand(ICommand parent, IList<ICommandListItem> listItems, int depth, int sessionId)
        {
            if (depth > MaxRecursionDepth)
            {
                throw new InvalidOperationException($"ネストが深すぎます (>{MaxRecursionDepth}) Line={parent.LineNumber}");
            }
            var commands = new List<ICommand>();
            var swPerItem = new Stopwatch();

            for (int i = 0; i < listItems.Count; i++)
            {
                if (listItems[i].IsEnable == false) continue;
                var listItem = listItems[i];
                swPerItem.Restart();
                var beforeIdx = i;
                var command = ItemToCommand(parent, listItem, listItems, ref i, depth, sessionId);
                swPerItem.Stop();
                if (command != null)
                {
                    commands.Add(command);
                    _logger?.LogTrace("[MacroFactory:{Session}] depth={Depth} idx={BeforeIdx}->{AfterIdx} type={Type} line={Line} created commandType={CmdType} elapsed={Ms}ms", sessionId, depth, beforeIdx, i, listItem.GetType().Name, listItem.LineNumber, command.GetType().Name, swPerItem.ElapsedMilliseconds);
                }
                else
                {
                    _logger?.LogTrace("[MacroFactory:{Session}] depth={Depth} idx={Idx} type={Type} line={Line} skipped elapsed={Ms}ms", sessionId, depth, beforeIdx, listItem.GetType().Name, listItem.LineNumber, swPerItem.ElapsedMilliseconds);
                }
                if (swPerItem.ElapsedMilliseconds > 500)
                {
                    _logger?.LogWarning("[MacroFactory:{Session}] Slow item convert >500ms depth={Depth} type={Type} line={Line} elapsed={Ms}ms", sessionId, depth, listItem.GetType().Name, listItem.LineNumber, swPerItem.ElapsedMilliseconds);
                }
            }
            return commands;
        }

        /// <summary>
        /// アイテムをコマンドに変換
        /// </summary>
        private static ICommand? ItemToCommand(ICommand parent, ICommandListItem listItem, IList<ICommandListItem> listItems, ref int index, int depth, int sessionId)
        {
            ICommand? command = null;
            try
            {
                if (_pluginService != null && !string.IsNullOrEmpty(listItem.ItemType) && listItem.ItemType.Contains('.'))
                {
                    var parts = listItem.ItemType.Split('.', 2);
                    if (parts.Length == 2)
                    {
                        var pluginId = parts[0];
                        var commandId = parts[1];
                        var pluginCommand = _pluginService.CreatePluginCommand(pluginId, commandId, parent, _serviceProvider);
                        if (pluginCommand != null)
                        {
                            _logger?.LogDebug("[MacroFactory:{Session}] プラグインコマンド作成: {PluginId}.{CommandId} line={Line}", sessionId, pluginId, commandId, listItem.LineNumber);
                            return pluginCommand;
                        }
                        else
                        {
                            _logger?.LogWarning("[MacroFactory:{Session}] プラグインコマンド未解決: {PluginId}.{CommandId} line={Line}", sessionId, pluginId, commandId, listItem.LineNumber);
                        }
                    }
                }
                switch (listItem)
                {
                    case WaitImageItem waitImageItem:
                        command = new WaitImageCommand(parent, waitImageItem, _serviceProvider);
                        break;
                    case ClickImageItem clickImageItem:
                        command = new ClickImageCommand(parent, clickImageItem, _serviceProvider);
                        break;
                    case ClickImageAIItem clickImageAIItem:
                        command = new ClickImageAICommand(parent, clickImageAIItem, _serviceProvider);
                        break;
                    case HotkeyItem hotkeyItem:
                        command = new HotkeyCommand(parent, hotkeyItem, _serviceProvider);
                        break;
                    case ClickItem clickItem:
                        command = new ClickCommand(parent, clickItem, _serviceProvider);
                        break;
                    case WaitItem waitItem:
                        command = new WaitCommand(parent, waitItem, _serviceProvider);
                        break;
                    case LoopItem loopItem:
                        {
                            var loopCommand = new LoopCommand(parent, loopItem, _serviceProvider) { LineNumber = loopItem.LineNumber };
                            if (loopItem.Pair == null) throw new InvalidOperationException($"Loop (行 {loopItem.LineNumber}) に対応するEndLoopがありません");
                            var childItems = GetChildrenListItems(listItem, listItems);
                            if (childItems.Count == 0) throw new InvalidOperationException($"Loop (行 {loopItem.LineNumber}) 内にコマンドがありません");
                            var childCommands = ListItemToCommand(loopCommand, childItems, depth + 1, sessionId);
                            foreach (var c in childCommands) loopCommand.AddChild(c);
                            index = GetItemIndex(listItems, loopItem.Pair);
                            command = loopCommand;
                        }
                        break;
                    case LoopBreakItem loopBreakItem:
                        command = new LoopBreakCommand(parent, loopBreakItem, _serviceProvider);
                        break;
                    case LoopEndItem:
                        break;
                    case IfImageExistItem ifImageExistItem:
                        {
                            var ifCommand = CreateIfCommandInstance(parent, ifImageExistItem);
                            if (ifImageExistItem.Pair == null) throw new InvalidOperationException($"IfImageExist (行 {ifImageExistItem.LineNumber}) に対応するEndIfがありません");
                            var childItems = GetChildrenListItems(listItem, listItems);
                            var childCommands = ListItemToCommand(ifCommand, childItems, depth + 1, sessionId);
                            foreach (var c in childCommands) ifCommand.AddChild(c);
                            index = GetItemIndex(listItems, ifImageExistItem.Pair);
                            command = ifCommand;
                        }
                        break;
                    case IfImageNotExistItem ifImageNotExistItem:
                        {
                            var ifCommand = CreateIfCommandInstance(parent, ifImageNotExistItem);
                            if (ifImageNotExistItem.Pair == null) throw new InvalidOperationException($"IfImageNotExist (行 {ifImageNotExistItem.LineNumber}) に対応するEndIfがありません");
                            var childItems = GetChildrenListItems(listItem, listItems);
                            var childCommands = ListItemToCommand(ifCommand, childItems, depth + 1, sessionId);
                            foreach (var c in childCommands) ifCommand.AddChild(c);
                            index = GetItemIndex(listItems, ifImageNotExistItem.Pair);
                            command = ifCommand;
                        }
                        break;
                    case IfImageExistAIItem ifImageExistAIItem:
                        {
                            var ifCommand = new IfImageExistAICommand(parent, ifImageExistAIItem, _serviceProvider);
                            if (ifImageExistAIItem.Pair == null) throw new InvalidOperationException($"IfImageExistAI (行 {ifImageExistAIItem.LineNumber}) に対応するEndIfがありません");
                            var childItems = GetChildrenListItems(listItem, listItems);
                            var childCommands = ListItemToCommand(ifCommand, childItems, depth + 1, sessionId);
                            foreach (var c in childCommands) ifCommand.AddChild(c);
                            index = GetItemIndex(listItems, ifImageExistAIItem.Pair);
                            command = ifCommand;
                        }
                        break;
                    case IfImageNotExistAIItem ifImageNotExistAIItem:
                        {
                            var ifCommand = new IfImageNotExistAICommand(parent, ifImageNotExistAIItem, _serviceProvider);
                            if (ifImageNotExistAIItem.Pair == null) throw new InvalidOperationException($"IfImageNotExistAI (行 {ifImageNotExistAIItem.LineNumber}) に対応するEndIfがありません");
                            var childItems = GetChildrenListItems(listItem, listItems);
                            var childCommands = ListItemToCommand(ifCommand, childItems, depth + 1, sessionId);
                            foreach (var c in childCommands) ifCommand.AddChild(c);
                            index = GetItemIndex(listItems, ifImageNotExistAIItem.Pair);
                            command = ifCommand;
                        }
                        break;
                    case IfVariableItem ifVariableItem:
                        {
                            var ifCommand = new IfVariableCommand(parent, ifVariableItem, _serviceProvider);
                            if (ifVariableItem.Pair == null) throw new InvalidOperationException($"IfVariable (行 {ifVariableItem.LineNumber}) に対応するEndIfがありません");
                            var childItems = GetChildrenListItems(listItem, listItems);
                            var childCommands = ListItemToCommand(ifCommand, childItems, depth + 1, sessionId);
                            foreach (var c in childCommands) ifCommand.AddChild(c);
                            index = GetItemIndex(listItems, ifVariableItem.Pair);
                            command = ifCommand;
                        }
                        break;
                    case IfEndItem:
                        break;
                    case ExecuteItem executeItem:
                        command = new ExecuteCommand(parent, executeItem, _serviceProvider);
                        break;
                    case SetVariableItem setVariableItem:
                        command = new SetVariableCommand(parent, setVariableItem, _serviceProvider);
                        break;
                    case SetVariableAIItem setVariableAIItem:
                        command = new SetVariableAICommand(parent, setVariableAIItem, _serviceProvider);
                        break;
                    case ScreenshotItem screenshotItem:
                        command = new ScreenshotCommand(parent, screenshotItem, _serviceProvider);
                        break;
                    default:
                        _logger?.LogWarning("[MacroFactory:{Session}] 未対応のコマンドタイプ: {CommandType}", sessionId, listItem.GetType().Name);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[MacroFactory:{Session}] コマンド作成中エラー type={Type} line={Line}", sessionId, listItem.GetType().Name, listItem.LineNumber);
                throw;
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
                IfImageExistItem ifImageExistItem => new IfImageExistCommand(parent, ifImageExistItem, _serviceProvider),
                IfImageNotExistItem ifImageNotExistItem => new IfImageNotExistCommand(parent, ifImageNotExistItem, _serviceProvider),
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
