using AutoTool.Command.Class;
using AutoTool.Model.List.Interface;
using AutoTool.Services.Plugin;
using AutoTool.Command.Interface;
using AutoTool.Model.List.Class;
using AutoTool.Model.CommandDefinition;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTool.Model.MacroFactory
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
        public static AutoTool.Command.Interface.ICommand CreateMacro(IEnumerable<ICommandListItem> items)
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

                // ルートコマンドのLineNumberを適切に設定（マクロ全体のエントリポイントとして0を使用）
                var root = new LoopCommand(null, new LoopCommandSettings() { LoopCount = 1 }, _serviceProvider)
                {
                    LineNumber = 0 // ルートコマンドは0に設定
                };

                var childSw = Stopwatch.StartNew();
                var childCommands = ListItemToCommand(root, cloneItems, 0, sessionId);
                foreach (var child in childCommands) root.AddChild(child);
                childSw.Stop();
                swTotal.Stop();
                
                _logger?.LogDebug("[MacroFactory:{Session}] CreateMacro complete Children={ChildCount} TotalElapsed={TotalMs}ms BuildElapsed={BuildMs}ms", sessionId, root.Children.Count(), swTotal.ElapsedMilliseconds, childSw.ElapsedMilliseconds);
                
                // デバッグ: 作成されたコマンドのLineNumberを検証
                ValidateCommandLineNumbers(root, sessionId);
                
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
        /// 作成されたコマンドのLineNumberを検証（デバッグ用）
        /// </summary>
        private static void ValidateCommandLineNumbers(AutoTool.Command.Interface.ICommand root, int sessionId)
        {
            try
            {
                var commands = new List<AutoTool.Command.Interface.ICommand>();
                CollectAllCommands(root, commands);
                
                var invalidCommands = commands.Where(c => c.LineNumber <= 0 && c != root).ToList();
                if (invalidCommands.Count > 0)
                {
                    _logger?.LogWarning("[MacroFactory:{Session}] LineNumber=0のコマンドが{Count}個見つかりました:", sessionId, invalidCommands.Count);
                    foreach (var cmd in invalidCommands.Take(5)) // 最初の5個まで表示
                    {
                        _logger?.LogWarning("[MacroFactory:{Session}]   {Type} (Line: {Line})", sessionId, cmd.GetType().Name, cmd.LineNumber);
                    }
                }
                else
                {
                    _logger?.LogDebug("[MacroFactory:{Session}] 全コマンドのLineNumber検証完了: {Total}個", sessionId, commands.Count);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[MacroFactory:{Session}] LineNumber検証中にエラー", sessionId);
            }
        }

        /// <summary>
        /// すべてのコマンドを再帰的に収集
        /// </summary>
        private static void CollectAllCommands(AutoTool.Command.Interface.ICommand command, List<AutoTool.Command.Interface.ICommand> commands)
        {
            commands.Add(command);
            foreach (var child in command.Children)
            {
                CollectAllCommands(child, commands);
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
        private static IEnumerable<AutoTool.Command.Interface.ICommand> ListItemToCommand(AutoTool.Command.Interface.ICommand parent, IList<ICommandListItem> listItems, int depth, int sessionId)
        {
            if (depth > MaxRecursionDepth)
            {
                throw new InvalidOperationException($"ネストレベルが過剰 (>{MaxRecursionDepth}) Line={parent.LineNumber}");
            }
            var commands = new List<AutoTool.Command.Interface.ICommand>();
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
        private static AutoTool.Command.Interface.ICommand? ItemToCommand(AutoTool.Command.Interface.ICommand parent, ICommandListItem listItem, IList<ICommandListItem> listItems, ref int index, int depth, int sessionId)
        {
            AutoTool.Command.Interface.ICommand? command = null;
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
                        if (pluginCommand is AutoTool.Command.Interface.ICommand autoToolCommand)
                        {
                            autoToolCommand.LineNumber = listItem.LineNumber; // LineNumber設定
                            _logger?.LogDebug("[MacroFactory:{Session}] プラグインコマンド生成: {PluginId}.{CommandId} line={Line}", sessionId, pluginId, commandId, listItem.LineNumber);
                            return autoToolCommand;
                        }
                        else
                        {
                            _logger?.LogWarning("[MacroFactory:{Session}] プラグインコマンド生成失敗: {PluginId}.{CommandId} line={Line}", sessionId, pluginId, commandId, listItem.LineNumber);
                        }
                    }
                }
                switch (listItem)
                {
                    case WaitImageItem waitImageItem:
                        command = new WaitImageCommand(parent, waitImageItem, _serviceProvider) { LineNumber = waitImageItem.LineNumber };
                        break;
                    case ClickImageItem clickImageItem:
                        command = new ClickImageCommand(parent, clickImageItem, _serviceProvider) { LineNumber = clickImageItem.LineNumber };
                        break;
                    case ClickImageAIItem clickImageAIItem:
                        command = new ClickImageAICommand(parent, clickImageAIItem, _serviceProvider) { LineNumber = clickImageAIItem.LineNumber };
                        break;
                    case HotkeyItem hotkeyItem:
                        command = new HotkeyCommand(parent, hotkeyItem, _serviceProvider) { LineNumber = hotkeyItem.LineNumber };
                        break;
                    case ClickItem clickItem:
                        command = new ClickCommand(parent, clickItem, _serviceProvider) { LineNumber = clickItem.LineNumber };
                        break;
                    case WaitItem waitItem:
                        command = new WaitCommand(parent, waitItem, _serviceProvider) { LineNumber = waitItem.LineNumber };
                        break;
                    case LoopItem loopItem:
                        {
                            var loopCommand = new LoopCommand(parent, loopItem, _serviceProvider) { LineNumber = loopItem.LineNumber };
                            if (loopItem.Pair == null) throw new InvalidOperationException($"Loop (行 {loopItem.LineNumber}) に対応するEndLoopが見つかりません");
                            var childItems = GetChildrenListItems(listItem, listItems);
                            if (childItems.Count == 0) throw new InvalidOperationException($"Loop (行 {loopItem.LineNumber}) 内にコマンドがありません");
                            var childCommands = ListItemToCommand(loopCommand, childItems, depth + 1, sessionId);
                            foreach (var c in childCommands) loopCommand.AddChild(c);
                            index = GetItemIndex(listItems, loopItem.Pair);
                            command = loopCommand;
                        }
                        break;
                    case LoopBreakItem loopBreakItem:
                        command = new LoopBreakCommand(parent, loopBreakItem, _serviceProvider) { LineNumber = loopBreakItem.LineNumber };
                        break;
                    case LoopEndItem:
                        break;
                    case IfImageExistItem ifImageExistItem:
                        {
                            var ifCommand = CreateIfCommandInstance(parent, ifImageExistItem);
                            ifCommand.LineNumber = ifImageExistItem.LineNumber; // LineNumber設定
                            if (ifImageExistItem.Pair == null) throw new InvalidOperationException($"IfImageExist (行 {ifImageExistItem.LineNumber}) に対応するEndIfが見つかりません");
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
                            ifCommand.LineNumber = ifImageNotExistItem.LineNumber; // LineNumber設定
                            if (ifImageNotExistItem.Pair == null) throw new InvalidOperationException($"IfImageNotExist (行 {ifImageNotExistItem.LineNumber}) に対応するEndIfが見つかりません");
                            var childItems = GetChildrenListItems(listItem, listItems);
                            var childCommands = ListItemToCommand(ifCommand, childItems, depth + 1, sessionId);
                            foreach (var c in childCommands) ifCommand.AddChild(c);
                            index = GetItemIndex(listItems, ifImageNotExistItem.Pair);
                            command = ifCommand;
                        }
                        break;
                    case IfImageExistAIItem ifImageExistAIItem:
                        {
                            var ifCommand = new IfImageExistAICommand(parent, ifImageExistAIItem, _serviceProvider) { LineNumber = ifImageExistAIItem.LineNumber };
                            if (ifImageExistAIItem.Pair == null) throw new InvalidOperationException($"IfImageExistAI (行 {ifImageExistAIItem.LineNumber}) に対応するEndIfが見つかりません");
                            var childItems = GetChildrenListItems(listItem, listItems);
                            var childCommands = ListItemToCommand(ifCommand, childItems, depth + 1, sessionId);
                            foreach (var c in childCommands) ifCommand.AddChild(c);
                            index = GetItemIndex(listItems, ifImageExistAIItem.Pair);
                            command = ifCommand;
                        }
                        break;
                    case IfImageNotExistAIItem ifImageNotExistAIItem:
                        {
                            var ifCommand = new IfImageNotExistAICommand(parent, ifImageNotExistAIItem, _serviceProvider) { LineNumber = ifImageNotExistAIItem.LineNumber };
                            if (ifImageNotExistAIItem.Pair == null) throw new InvalidOperationException($"IfImageNotExistAI (行 {ifImageNotExistAIItem.LineNumber}) に対応するEndIfが見つかりません");
                            var childItems = GetChildrenListItems(listItem, listItems);
                            var childCommands = ListItemToCommand(ifCommand, childItems, depth + 1, sessionId);
                            foreach (var c in childCommands) ifCommand.AddChild(c);
                            index = GetItemIndex(listItems, ifImageNotExistAIItem.Pair);
                            command = ifCommand;
                        }
                        break;
                    case IfVariableItem ifVariableItem:
                        {
                            var ifCommand = new IfVariableCommand(parent, ifVariableItem, _serviceProvider) { LineNumber = ifVariableItem.LineNumber };
                            if (ifVariableItem.Pair == null) throw new InvalidOperationException($"IfVariable (行 {ifVariableItem.LineNumber}) に対応するEndIfが見つかりません");
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
                        command = new ExecuteCommand(parent, executeItem, _serviceProvider) { LineNumber = executeItem.LineNumber };
                        break;
                    case SetVariableItem setVariableItem:
                        command = new SetVariableCommand(parent, setVariableItem, _serviceProvider) { LineNumber = setVariableItem.LineNumber };
                        break;
                    case SetVariableAIItem setVariableAIItem:
                        command = new SetVariableAICommand(parent, setVariableAIItem, _serviceProvider) { LineNumber = setVariableAIItem.LineNumber };
                        break;
                    case ScreenshotItem screenshotItem:
                        command = new ScreenshotCommand(parent, screenshotItem, _serviceProvider) { LineNumber = screenshotItem.LineNumber };
                        break;
                    default:
                        _logger?.LogWarning("[MacroFactory:{Session}] 未知のリストアイテム: {CommandType}", sessionId, listItem.GetType().Name);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[MacroFactory:{Session}] コマンド生成中にエラー発生 [ type={Type} line={Line}", sessionId, listItem.GetType().Name, listItem.LineNumber);
                throw;
            }
            return command;
        }

        /// <summary>
        /// If系コマンドのインスタンスを作成
        /// </summary>
        private static AutoTool.Command.Interface.ICommand CreateIfCommandInstance(AutoTool.Command.Interface.ICommand parent, ICommandListItem listItem)
        {
            AutoTool.Command.Interface.ICommand command = listItem switch
            {
                IfImageExistItem ifImageExistItem => new IfImageExistCommand(parent, ifImageExistItem, _serviceProvider),
                IfImageNotExistItem ifImageNotExistItem => new IfImageNotExistCommand(parent, ifImageNotExistItem, _serviceProvider),
                _ => throw new NotSupportedException($"未対応のIfアイテム: {listItem.GetType().Name}")
            };
            
            // LineNumberを設定
            command.LineNumber = listItem.LineNumber;
            return command;
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
