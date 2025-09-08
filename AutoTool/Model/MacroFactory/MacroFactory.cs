using AutoTool.Services.Plugin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoTool.Command.Base;
using AutoTool.Command.Definition;
using AutoTool.ViewModel.Shared;

namespace AutoTool.Model.MacroFactory
{
    /// <summary>
    /// マクロファクトリ（DirectCommandRegistry統合版）
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
        public static IAutoToolCommand CreateMacro(IEnumerable<UniversalCommandItem> items)
        {
            var sessionId = Interlocked.Increment(ref _buildSessionId);
            var swTotal = Stopwatch.StartNew();
            
            try
            {
                var originalList = items.ToList();
                var cloneSw = Stopwatch.StartNew();
                var cloneItems = originalList.Select(x => x.Clone()).ToList();
                cloneSw.Stop();
                
                _logger?.LogDebug("[MacroFactory:{Session}] CreateMacro start Items={Count} CloneElapsed={CloneMs}ms", 
                    sessionId, cloneItems.Count, cloneSw.ElapsedMilliseconds);

                // Pair再構築 (Cloneで元オブジェクトの参照をコピーしてしまいインデックス探索が失敗する問題対策)
                var pairFixSw = Stopwatch.StartNew();
                RebuildPairRelationships(cloneItems, originalList, sessionId);
                pairFixSw.Stop();
                
                _logger?.LogDebug("[MacroFactory:{Session}] Pair rebuild elapsed={Ms}ms", sessionId, pairFixSw.ElapsedMilliseconds);

                // ルートコマンド作成
                var root = new RootCommand(_serviceProvider)
                {
                    LineNumber = 0
                };

                // 子コマンドを作成
                var childSw = Stopwatch.StartNew();
                var childCommands = CreateChildCommands(root, cloneItems, 0, sessionId);
                foreach (var child in childCommands) 
                {
                    root.AddChild(child);
                }
                childSw.Stop();
                swTotal.Stop();
                
                _logger?.LogDebug("[MacroFactory:{Session}] CreateMacro complete Children={ChildCount} TotalElapsed={TotalMs}ms BuildElapsed={BuildMs}ms", 
                    sessionId, root.Children.Count(), swTotal.ElapsedMilliseconds, childSw.ElapsedMilliseconds);
                
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
        /// 子コマンドを作成
        /// </summary>
        private static IEnumerable<IAutoToolCommand> CreateChildCommands(
            IAutoToolCommand parent, 
            IList<UniversalCommandItem> listItems, 
            int depth, 
            int sessionId)
        {
            if (depth > MaxRecursionDepth)
            {
                throw new InvalidOperationException($"ネストレベルが過剰 (>{MaxRecursionDepth}) Line={parent.LineNumber}");
            }

            var commands = new List<IAutoToolCommand>();
            var swPerItem = new Stopwatch();

            for (int i = 0; i < listItems.Count; i++)
            {
                var listItem = listItems[i];
                if (!listItem.IsEnable) continue;

                swPerItem.Restart();
                var beforeIdx = i;
                var command = CreateCommandFromItem(parent, listItem, listItems, ref i, depth, sessionId);
                swPerItem.Stop();

                if (command != null)
                {
                    commands.Add(command);
                    _logger?.LogTrace("[MacroFactory:{Session}] depth={Depth} idx={BeforeIdx}->{AfterIdx} type={Type} line={Line} created commandType={CmdType} elapsed={Ms}ms", 
                        sessionId, depth, beforeIdx, i, listItem.GetType().Name, listItem.LineNumber, command.GetType().Name, swPerItem.ElapsedMilliseconds);
                }
                else
                {
                    _logger?.LogTrace("[MacroFactory:{Session}] depth={Depth} idx={Idx} type={Type} line={Line} skipped elapsed={Ms}ms", 
                        sessionId, depth, beforeIdx, listItem.GetType().Name, listItem.LineNumber, swPerItem.ElapsedMilliseconds);
                }

                if (swPerItem.ElapsedMilliseconds > 500)
                {
                    _logger?.LogWarning("[MacroFactory:{Session}] Slow item convert >500ms depth={Depth} type={Type} line={Line} elapsed={Ms}ms", 
                        sessionId, depth, listItem.GetType().Name, listItem.LineNumber, swPerItem.ElapsedMilliseconds);
                }
            }

            return commands;
        }

        /// <summary>
        /// アイテムからコマンドを作成（DirectCommandRegistry統合版）
        /// </summary>
        private static IAutoToolCommand? CreateCommandFromItem(
            IAutoToolCommand parent,
            UniversalCommandItem listItem, 
            IList<UniversalCommandItem> listItems, 
            ref int index, 
            int depth, 
            int sessionId)
        {
            try
            {
                // 1. プラグインコマンドの処理
                if (_pluginService != null && !string.IsNullOrEmpty(listItem.ItemType) && listItem.ItemType.Contains('.'))
                {
                    var parts = listItem.ItemType.Split('.', 2);
                    if (parts.Length == 2)
                    {
                        var pluginId = parts[0];
                        var commandId = parts[1];
                        var pluginCommand = _pluginService.CreatePluginCommand(pluginId, commandId, parent, _serviceProvider);
                        if (pluginCommand is IAutoToolCommand autoToolCommand)
                        {
                            autoToolCommand.LineNumber = listItem.LineNumber;
                            _logger?.LogDebug("[MacroFactory:{Session}] プラグインコマンド生成: {PluginId}.{CommandId} line={Line}", 
                                sessionId, pluginId, commandId, listItem.LineNumber);
                            return autoToolCommand;
                        }
                    }
                }


                // 3. DirectCommandRegistryでコマンド作成
                var command = AutoToolCommandRegistry.CreateCommand(listItem.ItemType, parent, listItem, _serviceProvider!);
                if (command != null)
                {
                    command.LineNumber = listItem.LineNumber;
                    command.IsEnabled = listItem.IsEnable;
                    command.NestLevel = depth;

                    _logger?.LogTrace("[MacroFactory:{Session}] DirectCommandRegistryでコマンド生成: {ItemType} -> {CommandType} line={Line}", 
                        sessionId, listItem.ItemType, command.GetType().Name, listItem.LineNumber);

                    // 複雑なコマンド（Loop、If）の子要素処理
                    if (IsComplexCommand(listItem.ItemType))
                    {
                        ProcessComplexCommand(command, listItem, listItems, ref index, depth, sessionId);
                    }

                    return command;
                }

                _logger?.LogWarning("[MacroFactory:{Session}] 未対応のコマンドタイプ: {ItemType} line={Line}", 
                    sessionId, listItem.ItemType, listItem.LineNumber);
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[MacroFactory:{Session}] コマンド生成中にエラー発生 type={Type} line={Line}", 
                    sessionId, listItem.GetType().Name, listItem.LineNumber);
                throw;
            }
        }

        /// <summary>
        /// 複雑なコマンド（Loop、If）かどうかを判定
        /// </summary>
        private static bool IsComplexCommand(string itemType)
        {
            return itemType switch
            {
                "Loop" => true,
                "IfImageExist" => true,
                "IfImageNotExist" => true,
                "IfImageExist_AI" => true,
                "IfImageNotExist_AI" => true,
                "IfVariable" => true,
                _ => false
            };
        }

        /// <summary>
        /// 複雑なコマンドの子要素処理
        /// </summary>
        private static void ProcessComplexCommand(
            IAutoToolCommand command, 
            UniversalCommandItem item, 
            IList<UniversalCommandItem> listItems, 
            ref int index, 
            int depth, 
            int sessionId)
        {
            var endCommandType = GetEndCommandType(item.ItemType);
            if (string.IsNullOrEmpty(endCommandType))
            {
                return;
            }

            index++; // 開始コマンドの次から処理開始

            // 子要素を処理
            while (index < listItems.Count)
            {
                var childItem = listItems[index];

                // 終了コマンドに達したら終了
                if (childItem.ItemType == endCommandType)
                {
                    break;
                }

                var childCommand = CreateCommandFromItem(command, childItem, listItems, ref index, depth + 1, sessionId);
                if (childCommand != null)
                {
                    command.AddChild(childCommand);
                }

                // 子要素が複雑なコマンドでない場合はインデックスを進める
                if (!IsComplexCommand(childItem.ItemType))
                {
                    index++;
                }
            }
        }

        /// <summary>
        /// 開始コマンドに対応する終了コマンドタイプを取得
        /// </summary>
        private static string GetEndCommandType(string startCommandType)
        {
            return startCommandType switch
            {
                "Loop" => "LoopEnd",
                "IfImageExist" => "IfEnd",
                "IfImageNotExist" => "IfEnd",
                "IfImageExistAI" => "IfEnd",
                "IfImageNotExistAI" => "IfEnd",
                "IfVariable" => "IfEnd",
                _ => string.Empty
            };
        }

        /// <summary>
        /// UniversalCommandItemをUniversalCommandItemに変換
        /// </summary>
        private static UniversalCommandItem ConvertToUniversalItem(UniversalCommandItem item)
        {
            if (item is UniversalCommandItem universalItem)
            {
                return universalItem;
            }

            // 古いアイテムからUniversalCommandItemを作成
            var newItem = new UniversalCommandItem
            {
                ItemType = item.ItemType,
                IsEnable = item.IsEnable,
                LineNumber = item.LineNumber,
                IsRunning = item.IsRunning,
                IsSelected = item.IsSelected,
                Description = item.Description,
                Comment = item.Comment,
                NestLevel = item.NestLevel,
                IsInLoop = item.IsInLoop,
                IsInIf = item.IsInIf,
                Progress = item.Progress
            };

            // Pairプロパティが存在する場合はコピー
            var pairProperty = item.GetType().GetProperty("Pair");
            if (pairProperty != null)
            {
                var pairValue = pairProperty.GetValue(item) as UniversalCommandItem;
                newItem.Pair = pairValue;
            }

            // 設定定義を初期化
            newItem.InitializeSettingDefinitions();
            
            _logger?.LogTrace("古いアイテムをUniversalCommandItemに変換: {ItemType} line={Line}", 
                item.ItemType, item.LineNumber);

            return newItem;
        }

        /// <summary>
        /// Clone後に失われた Pair 関係を LineNumber ベースで再構築
        /// </summary>
        private static void RebuildPairRelationships(IList<UniversalCommandItem> cloned, IList<UniversalCommandItem> original, int sessionId)
        {
            try
            {
                var lineToClone = cloned.GroupBy(c => c.LineNumber).ToDictionary(g => g.Key, g => g.First());
                var lineToOrig = original.GroupBy(o => o.LineNumber).ToDictionary(g => g.Key, g => g.First());
                int fixedCount = 0, missingPair = 0, unresolved = 0;

                foreach (var clone in cloned)
                {
                    var pairProp = clone.GetType().GetProperty("Pair");
                    if (pairProp == null) continue;

                    // 元アイテム取得
                    if (!lineToOrig.TryGetValue(clone.LineNumber, out var origItem))
                    {
                        unresolved++;
                        continue;
                    }

                    var origPair = pairProp.GetValue(origItem) as UniversalCommandItem;
                    if (origPair == null)
                    {
                        missingPair++;
                        continue;
                    }

                    if (!lineToClone.TryGetValue(origPair.LineNumber, out var clonePair))
                    {
                        unresolved++;
                        continue;
                    }

                    // 既に正しいならスキップ
                    var currentPair = pairProp.GetValue(clone) as UniversalCommandItem;
                    if (currentPair != clonePair)
                    {
                        pairProp.SetValue(clone, clonePair);
                        fixedCount++;
                    }
                }

                _logger?.LogDebug("[MacroFactory:{Session}] Pair rebuild result Fixed={Fixed} MissingOrigPair={Missing} Unresolved={Unresolved}", 
                    sessionId, fixedCount, missingPair, unresolved);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[MacroFactory:{Session}] Pair rebuild でエラー", sessionId);
            }
        }

        /// <summary>
        /// 作成されたコマンドのLineNumberを検証（デバッグ用）
        /// </summary>
        private static void ValidateCommandLineNumbers(IAutoToolCommand root, int sessionId)
        {
            try
            {
                var commands = new List<IAutoToolCommand>();
                CollectAllCommands(root, commands);
                
                var invalidCommands = commands.Where(c => c.LineNumber <= 0 && c != root).ToList();
                if (invalidCommands.Count > 0)
                {
                    _logger?.LogWarning("[MacroFactory:{Session}] LineNumber=0のコマンドが{Count}個見つかりました:", sessionId, invalidCommands.Count);
                    foreach (var cmd in invalidCommands.Take(5))
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
        private static void CollectAllCommands(IAutoToolCommand command, List<IAutoToolCommand> commands)
        {
            commands.Add(command);
            foreach (var child in command.Children)
            {
                CollectAllCommands(child, commands);
            }
        }
    }
}
