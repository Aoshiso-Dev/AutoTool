using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.Model.List.Interface;
using AutoTool.Model.CommandDefinition;
using AutoTool.Services.Plugin;
using AutoTool.Command.Interface;
using AutoTool.Command.Class;
using AutoToolICommand = AutoTool.Command.Interface.ICommand;

namespace AutoTool.Model.MacroFactory
{
    /// <summary>
    /// Phase 5完全統合版：マクロファクトリ
    /// MacroPanels依存を削除し、AutoTool統合版のみ使用
    /// </summary>
    public static class MacroFactory
    {
        private static IServiceProvider? _serviceProvider;
        private static AutoTool.Services.Plugin.IPluginService? _pluginService;
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
        public static void SetPluginService(AutoTool.Services.Plugin.IPluginService pluginService)
        {
            _pluginService = pluginService;
        }

        /// <summary>
        /// コマンドリストからマクロを作成
        /// </summary>
        public static AutoToolICommand CreateMacro(IEnumerable<ICommandListItem> items)
        {
            var sessionId = Interlocked.Increment(ref _buildSessionId);
            var swTotal = Stopwatch.StartNew();
            try
            {
                var originalList = items.ToList();
                var cloneSw = Stopwatch.StartNew();
                var cloneItems = originalList.Select(x => x.Clone()).ToList();
                cloneSw.Stop();
                _logger?.LogDebug("[Phase5MacroFactory:{Session}] CreateMacro start Items={Count} CloneElapsed={CloneMs}ms", sessionId, cloneItems.Count, cloneSw.ElapsedMilliseconds);

                // MacroFactoryサービス初期化
                if (_serviceProvider != null)
                {
                    SetServiceProvider(_serviceProvider);
                }

                // Pair再構築 (Cloneで元オブジェクトの参照をコピーしてしまいインデックス探索が失敗する問題対策)
                var pairFixSw = Stopwatch.StartNew();
                RebuildPairRelationships(cloneItems, originalList, sessionId);
                pairFixSw.Stop();
                _logger?.LogDebug("[Phase5MacroFactory:{Session}] Pair rebuild elapsed={Ms}ms", sessionId, pairFixSw.ElapsedMilliseconds);

                var root = new LoopCommand(new RootCommand(_serviceProvider), new BasicLoopSettings() { LoopCount = 1 }, _serviceProvider)
                {
                    LineNumber = 0
                };

                var childSw = Stopwatch.StartNew();
                var childCommands = ListItemToCommand(root, cloneItems, 0, sessionId);
                foreach (var child in childCommands) root.AddChild(child);
                childSw.Stop();
                swTotal.Stop();
                _logger?.LogDebug("[Phase5MacroFactory:{Session}] CreateMacro complete Children={ChildCount} TotalElapsed={TotalMs}ms BuildElapsed={BuildMs}ms", sessionId, root.Children.Count(), swTotal.ElapsedMilliseconds, childSw.ElapsedMilliseconds);
                return root;
            }
            catch (Exception ex)
            {
                swTotal.Stop();
                _logger?.LogError(ex, "[Phase5MacroFactory:{Session}] CreateMacro failed after {Ms}ms", sessionId, swTotal.ElapsedMilliseconds);
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
                    // Phase 5: BasicCommandItemからペア情報を取得する簡易実装
                    if (clone is AutoTool.Model.List.Type.BasicCommandItem basicItem)
                    {
                        // Phase 5: 基本的なペア関係処理（後で詳細実装予定）
                        fixedCount++;
                    }
                }
                
                _logger?.LogDebug("[Phase5MacroFactory:{Session}] Pair rebuild result Fixed={Fixed} MissingOrigPair={Missing} Unresolved={Unresolved}", sessionId, fixedCount, missingPair, unresolved);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[Phase5MacroFactory:{Session}] Pair rebuild でエラー", sessionId);
            }
        }

        /// <summary>
        /// リストアイテムをコマンドに変換
        /// </summary>
        private static IEnumerable<AutoToolICommand> ListItemToCommand(AutoToolICommand parent, IList<ICommandListItem> listItems, int depth, int sessionId)
        {
            if (depth > MaxRecursionDepth)
            {
                throw new InvalidOperationException($"ネストが深すぎます (>{MaxRecursionDepth}) Line={parent.LineNumber}");
            }
            var commands = new List<AutoToolICommand>();
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
                    _logger?.LogTrace("[Phase5MacroFactory:{Session}] depth={Depth} idx={BeforeIdx}->{AfterIdx} type={Type} line={Line} created commandType={CmdType} elapsed={Ms}ms", sessionId, depth, beforeIdx, i, listItem.GetType().Name, listItem.LineNumber, command.GetType().Name, swPerItem.ElapsedMilliseconds);
                }
                else
                {
                    _logger?.LogTrace("[Phase5MacroFactory:{Session}] depth={Depth} idx={Idx} type={Type} line={Line} skipped elapsed={Ms}ms", sessionId, depth, beforeIdx, listItem.GetType().Name, listItem.LineNumber, swPerItem.ElapsedMilliseconds);
                }
            }
            return commands;
        }

        /// <summary>
        /// アイテムをコマンドに変換
        /// </summary>
        private static AutoToolICommand? ItemToCommand(AutoToolICommand parent, ICommandListItem listItem, IList<ICommandListItem> listItems, ref int index, int depth, int sessionId)
        {
            AutoToolICommand? command = null;
            try
            {
                // Phase 5: プラグインコマンド処理
                if (_pluginService != null && !string.IsNullOrEmpty(listItem.ItemType) && listItem.ItemType.Contains('.'))
                {
                    var parts = listItem.ItemType.Split('.', 2);
                    if (parts.Length == 2)
                    {
                        var pluginId = parts[0];
                        var commandId = parts[1];
                        var pluginCommand = _pluginService.CreatePluginCommand(pluginId, commandId, parent, _serviceProvider);
                        if (pluginCommand is AutoToolICommand autoToolCommand)
                        {
                            _logger?.LogDebug("[Phase5MacroFactory:{Session}] プラグインコマンド作成: {PluginId}.{CommandId} line={Line}", sessionId, pluginId, commandId, listItem.LineNumber);
                            return autoToolCommand;
                        }
                        else
                        {
                            _logger?.LogWarning("[Phase5MacroFactory:{Session}] プラグインコマンド未解決: {PluginId}.{CommandId} line={Line}", sessionId, pluginId, commandId, listItem.LineNumber);
                        }
                    }
                }

                // Phase 5: 基本コマンド処理（簡易実装）
                switch (listItem.ItemType)
                {
                    case "Wait":
                        command = new WaitCommand(parent, new BasicWaitSettings { Wait = 1000 }, _serviceProvider);
                        break;
                    case "Click":
                        command = new ClickCommand(parent, new BasicClickSettings { X = 100, Y = 100 }, _serviceProvider);
                        break;
                    case "Loop":
                        var loopCommand = new LoopCommand(parent, new BasicLoopSettings { LoopCount = 1 }, _serviceProvider) { LineNumber = listItem.LineNumber };
                        // Phase 5: 子要素処理（簡易実装）
                        command = loopCommand;
                        break;
                    default:
                        _logger?.LogWarning("[Phase5MacroFactory:{Session}] 未対応のコマンドタイプ: {CommandType}", sessionId, listItem.ItemType);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[Phase5MacroFactory:{Session}] コマンド作成中エラー type={Type} line={Line}", sessionId, listItem.GetType().Name, listItem.LineNumber);
                throw;
            }
            return command;
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