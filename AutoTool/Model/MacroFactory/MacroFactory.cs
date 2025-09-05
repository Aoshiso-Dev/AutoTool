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
    /// �}�N���t�@�N�g���iDI,Plugin�����Łj
    /// </summary>
    public static class MacroFactory
    {
        private static IServiceProvider? _serviceProvider;
        private static IPluginService? _pluginService;
        private static ILogger? _logger;
        private static int _buildSessionId = 0;
        private const int MaxRecursionDepth = 100;

        /// <summary>
        /// �T�[�r�X�v���o�C�_�[��ݒ�
        /// </summary>
        public static void SetServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("MacroFactory");
        }

        /// <summary>
        /// �v���O�C���T�[�r�X��ݒ�
        /// </summary>
        public static void SetPluginService(IPluginService pluginService)
        {
            _pluginService = pluginService;
        }

        /// <summary>
        /// �R�}���h���X�g����}�N�����쐬
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

                // Pair�č\�z (Clone�Ō��I�u�W�F�N�g�̎Q�Ƃ��R�s�[���Ă��܂��C���f�b�N�X�T�������s������΍�)
                var pairFixSw = Stopwatch.StartNew();
                RebuildPairRelationships(cloneItems, originalList, sessionId);
                pairFixSw.Stop();
                _logger?.LogDebug("[MacroFactory:{Session}] Pair rebuild elapsed={Ms}ms", sessionId, pairFixSw.ElapsedMilliseconds);

                // ���[�g�R�}���h��LineNumber��K�؂ɐݒ�i�}�N���S�̂̃G���g���|�C���g�Ƃ���0���g�p�j
                var root = new LoopCommand(null, new LoopCommandSettings() { LoopCount = 1 }, _serviceProvider)
                {
                    LineNumber = 0 // ���[�g�R�}���h��0�ɐݒ�
                };

                var childSw = Stopwatch.StartNew();
                var childCommands = ListItemToCommand(root, cloneItems, 0, sessionId);
                foreach (var child in childCommands) root.AddChild(child);
                childSw.Stop();
                swTotal.Stop();
                
                _logger?.LogDebug("[MacroFactory:{Session}] CreateMacro complete Children={ChildCount} TotalElapsed={TotalMs}ms BuildElapsed={BuildMs}ms", sessionId, root.Children.Count(), swTotal.ElapsedMilliseconds, childSw.ElapsedMilliseconds);
                
                // �f�o�b�O: �쐬���ꂽ�R�}���h��LineNumber������
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
        /// �쐬���ꂽ�R�}���h��LineNumber�����؁i�f�o�b�O�p�j
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
                    _logger?.LogWarning("[MacroFactory:{Session}] LineNumber=0�̃R�}���h��{Count}������܂���:", sessionId, invalidCommands.Count);
                    foreach (var cmd in invalidCommands.Take(5)) // �ŏ���5�܂ŕ\��
                    {
                        _logger?.LogWarning("[MacroFactory:{Session}]   {Type} (Line: {Line})", sessionId, cmd.GetType().Name, cmd.LineNumber);
                    }
                }
                else
                {
                    _logger?.LogDebug("[MacroFactory:{Session}] �S�R�}���h��LineNumber���؊���: {Total}��", sessionId, commands.Count);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[MacroFactory:{Session}] LineNumber���ؒ��ɃG���[", sessionId);
            }
        }

        /// <summary>
        /// ���ׂẴR�}���h���ċA�I�Ɏ��W
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
        /// Clone��Ɏ���ꂽ Pair �֌W�� LineNumber �x�[�X�ōč\�z
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
                    if (pairProp == null) continue; // �ΏۊO

                    // ���A�C�e���擾
                    if (!lineToOrig.TryGetValue(clone.LineNumber, out var origItem))
                    {
                        unresolved++;
                        continue;
                    }
                    var origPair = pairProp.GetValue(origItem) as ICommandListItem;
                    if (origPair == null)
                    {
                        missingPair++;
                        continue; // Pair����
                    }
                    if (!lineToClone.TryGetValue(origPair.LineNumber, out var clonePair))
                    {
                        unresolved++;
                        continue; // �Ή��N���[��������
                    }
                    // ���ɐ������Ȃ�X�L�b�v
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
                _logger?.LogWarning(ex, "[MacroFactory:{Session}] Pair rebuild �ŃG���[", sessionId);
            }
        }

        /// <summary>
        /// ���X�g�A�C�e�����R�}���h�ɕϊ�
        /// </summary>
        private static IEnumerable<AutoTool.Command.Interface.ICommand> ListItemToCommand(AutoTool.Command.Interface.ICommand parent, IList<ICommandListItem> listItems, int depth, int sessionId)
        {
            if (depth > MaxRecursionDepth)
            {
                throw new InvalidOperationException($"�l�X�g���x�����ߏ� (>{MaxRecursionDepth}) Line={parent.LineNumber}");
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
        /// �A�C�e�����R�}���h�ɕϊ��i���I�V�X�e���Ή��Łj
        /// </summary>
        private static AutoTool.Command.Interface.ICommand? ItemToCommand(AutoTool.Command.Interface.ICommand parent, ICommandListItem listItem, IList<ICommandListItem> listItems, ref int index, int depth, int sessionId)
        {
            AutoTool.Command.Interface.ICommand? command = null;
            try
            {
                // 1. �v���O�C���R�}���h�̏���
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
                            autoToolCommand.LineNumber = listItem.LineNumber;
                            _logger?.LogDebug("[MacroFactory:{Session}] �v���O�C���R�}���h����: {PluginId}.{CommandId} line={Line}", sessionId, pluginId, commandId, listItem.LineNumber);
                            return autoToolCommand;
                        }
                    }
                }

                // 2. UniversalCommandItem�̓��I�V�X�e������
                if (listItem is UniversalCommandItem universalItem)
                {
                    command = DirectCommandRegistry.CreateCommand(universalItem.ItemType, parent, universalItem, _serviceProvider!);
                    if (command != null)
                    {
                        command.LineNumber = listItem.LineNumber;
                        _logger?.LogDebug("[MacroFactory:{Session}] ���I�V�X�e���ŃR�}���h����: {ItemType} -> {CommandType} line={Line}", 
                            sessionId, listItem.ItemType, command.GetType().Name, listItem.LineNumber);

                        // ���G�ȃR�}���h�iLoop�AIf�j�̎q�v�f����
                        if (IsComplexCommand(universalItem))
                        {
                            ProcessComplexCommand(command, universalItem, listItems, ref index, depth, sessionId);
                        }

                        return command;
                    }
                }

                // 3. �����̌�CommandListItem�̏����i����݊����j
                command = CreateLegacyCommand(parent, listItem, listItems, ref index, depth, sessionId);
                return command;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[MacroFactory:{Session}] �R�}���h�������ɃG���[���� [ type={Type} line={Line}", sessionId, listItem.GetType().Name, listItem.LineNumber);
                throw;
            }
        }

        /// <summary>
        /// ���G�ȃR�}���h�iLoop�AIf�j���ǂ����𔻒�
        /// </summary>
        private static bool IsComplexCommand(UniversalCommandItem item)
        {
            return item.ItemType switch
            {
                "Loop" => true,
                "IF_ImageExist" => true,
                "IF_ImageNotExist" => true,
                "IF_ImageExist_AI" => true,
                "IF_ImageNotExist_AI" => true,
                "IF_Variable" => true,
                _ => false
            };
        }

        /// <summary>
        /// ���G�ȃR�}���h�̎q�v�f����
        /// </summary>
        private static void ProcessComplexCommand(AutoTool.Command.Interface.ICommand command, UniversalCommandItem item, IList<ICommandListItem> listItems, ref int index, int depth, int sessionId)
        {
            if (item.Pair == null)
            {
                throw new InvalidOperationException($"{item.ItemType} (�s {item.LineNumber}) �ɑΉ�����I���R�}���h��������܂���");
            }

            var childItems = GetChildrenListItems(item, listItems);
            var childCommands = ListItemToCommand(command, childItems, depth + 1, sessionId);
            
            foreach (var c in childCommands) 
            {
                command.AddChild(c);
            }
            
            index = GetItemIndex(listItems, item.Pair);
        }

        /// <summary>
        /// �����̌�CommandListItem�̏����i����݊����j
        /// </summary>
        private static AutoTool.Command.Interface.ICommand? CreateLegacyCommand(AutoTool.Command.Interface.ICommand parent, ICommandListItem listItem, IList<ICommandListItem> listItems, ref int index, int depth, int sessionId)
        {
            // ������switch�������̂܂ܕێ��i����݊����̂��߁j
            return listItem switch
            {
                WaitImageItem waitImageItem => new WaitImageCommand(parent, waitImageItem, _serviceProvider) { LineNumber = waitImageItem.LineNumber },
                ClickImageItem clickImageItem => new ClickImageCommand(parent, clickImageItem, _serviceProvider) { LineNumber = clickImageItem.LineNumber },
                ClickImageAIItem clickImageAIItem => new ClickImageAICommand(parent, clickImageAIItem, _serviceProvider) { LineNumber = clickImageAIItem.LineNumber },
                HotkeyItem hotkeyItem => new HotkeyCommand(parent, hotkeyItem, _serviceProvider) { LineNumber = hotkeyItem.LineNumber },
                ClickItem clickItem => new ClickCommand(parent, clickItem, _serviceProvider) { LineNumber = clickItem.LineNumber },
                WaitItem waitItem => new WaitCommand(parent, waitItem, _serviceProvider) { LineNumber = waitItem.LineNumber },
                LoopItem loopItem => ProcessLoopItem(parent, loopItem, listItems, ref index, depth, sessionId),
                LoopBreakItem loopBreakItem => new LoopBreakCommand(parent, loopBreakItem, _serviceProvider) { LineNumber = loopBreakItem.LineNumber },
                LoopEndItem => null,
                IfImageExistItem ifImageExistItem => ProcessIfItem(parent, ifImageExistItem, listItems, ref index, depth, sessionId),
                IfImageNotExistItem ifImageNotExistItem => ProcessIfItem(parent, ifImageNotExistItem, listItems, ref index, depth, sessionId),
                IfImageExistAIItem ifImageExistAIItem => ProcessIfAIItem(parent, ifImageExistAIItem, listItems, ref index, depth, sessionId),
                IfImageNotExistAIItem ifImageNotExistAIItem => ProcessIfAIItem(parent, ifImageNotExistAIItem, listItems, ref index, depth, sessionId),
                IfVariableItem ifVariableItem => ProcessIfVariableItem(parent, ifVariableItem, listItems, ref index, depth, sessionId),
                IfEndItem => null,
                ExecuteItem executeItem => new ExecuteCommand(parent, executeItem, _serviceProvider) { LineNumber = executeItem.LineNumber },
                SetVariableItem setVariableItem => new SetVariableCommand(parent, setVariableItem, _serviceProvider) { LineNumber = setVariableItem.LineNumber },
                SetVariableAIItem setVariableAIItem => new SetVariableAICommand(parent, setVariableAIItem, _serviceProvider) { LineNumber = setVariableAIItem.LineNumber },
                ScreenshotItem screenshotItem => new ScreenshotCommand(parent, screenshotItem, _serviceProvider) { LineNumber = screenshotItem.LineNumber },
                _ => null
            };
        }

        // �����̃w���p�[���\�b�h�͕ێ�...
        private static AutoTool.Command.Interface.ICommand ProcessLoopItem(AutoTool.Command.Interface.ICommand parent, LoopItem loopItem, IList<ICommandListItem> listItems, ref int index, int depth, int sessionId)
        {
            var loopCommand = new LoopCommand(parent, loopItem, _serviceProvider) { LineNumber = loopItem.LineNumber };
            if (loopItem.Pair == null) throw new InvalidOperationException($"Loop (�s {loopItem.LineNumber}) �ɑΉ�����EndLoop��������܂���");
            var childItems = GetChildrenListItems(loopItem, listItems);
            if (childItems.Count == 0) throw new InvalidOperationException($"Loop (�s {loopItem.LineNumber}) ���ɃR�}���h������܂���");
            var childCommands = ListItemToCommand(loopCommand, childItems, depth + 1, sessionId);
            foreach (var c in childCommands) loopCommand.AddChild(c);
            index = GetItemIndex(listItems, loopItem.Pair);
            return loopCommand;
        }

        private static AutoTool.Command.Interface.ICommand ProcessIfItem(AutoTool.Command.Interface.ICommand parent, ICommandListItem ifItem, IList<ICommandListItem> listItems, ref int index, int depth, int sessionId)
        {
            var ifCommand = CreateIfCommandInstance(parent, ifItem);
            ifCommand.LineNumber = ifItem.LineNumber;
            
            var pairProperty = ifItem.GetType().GetProperty("Pair");
            var pair = pairProperty?.GetValue(ifItem) as ICommandListItem;
            if (pair == null) throw new InvalidOperationException($"{ifItem.ItemType} (�s {ifItem.LineNumber}) �ɑΉ�����EndIf��������܂���");
            
            var childItems = GetChildrenListItems(ifItem, listItems);
            var childCommands = ListItemToCommand(ifCommand, childItems, depth + 1, sessionId);
            foreach (var c in childCommands) ifCommand.AddChild(c);
            index = GetItemIndex(listItems, pair);
            return ifCommand;
        }

        private static AutoTool.Command.Interface.ICommand ProcessIfAIItem(AutoTool.Command.Interface.ICommand parent, ICommandListItem ifAIItem, IList<ICommandListItem> listItems, ref int index, int depth, int sessionId)
        {
            AutoTool.Command.Interface.ICommand ifCommand = ifAIItem switch
            {
                IfImageExistAIItem ifImageExistAIItem => new IfImageExistAICommand(parent, ifImageExistAIItem, _serviceProvider),
                IfImageNotExistAIItem ifImageNotExistAIItem => new IfImageNotExistAICommand(parent, ifImageNotExistAIItem, _serviceProvider),
                _ => throw new NotSupportedException($"���Ή���IfAI�A�C�e��: {ifAIItem.GetType().Name}")
            };
            
            ifCommand.LineNumber = ifAIItem.LineNumber;
            
            var pairProperty = ifAIItem.GetType().GetProperty("Pair");
            var pair = pairProperty?.GetValue(ifAIItem) as ICommandListItem;
            if (pair == null) throw new InvalidOperationException($"{ifAIItem.ItemType} (�s {ifAIItem.LineNumber}) �ɑΉ�����EndIf��������܂���");
            
            var childItems = GetChildrenListItems(ifAIItem, listItems);
            var childCommands = ListItemToCommand(ifCommand, childItems, depth + 1, sessionId);
            foreach (var c in childCommands) ifCommand.AddChild(c);
            index = GetItemIndex(listItems, pair);
            return ifCommand;
        }

        private static AutoTool.Command.Interface.ICommand ProcessIfVariableItem(AutoTool.Command.Interface.ICommand parent, IfVariableItem ifVariableItem, IList<ICommandListItem> listItems, ref int index, int depth, int sessionId)
        {
            var ifCommand = new IfVariableCommand(parent, ifVariableItem, _serviceProvider) { LineNumber = ifVariableItem.LineNumber };
            if (ifVariableItem.Pair == null) throw new InvalidOperationException($"IfVariable (�s {ifVariableItem.LineNumber}) �ɑΉ�����EndIf��������܂���");
            var childItems = GetChildrenListItems(ifVariableItem, listItems);
            var childCommands = ListItemToCommand(ifCommand, childItems, depth + 1, sessionId);
            foreach (var c in childCommands) ifCommand.AddChild(c);
            index = GetItemIndex(listItems, ifVariableItem.Pair);
            return ifCommand;
        }

        /// <summary>
        /// �q�A�C�e���̃��X�g���擾
        /// </summary>
        private static IList<ICommandListItem> GetChildrenListItems(ICommandListItem listItem, IList<ICommandListItem> listItems)
        {
            var childrenListItems = new List<ICommandListItem>();

            ICommandListItem? endItem = null;

            // ILoopItem�܂���IIfItem����I���A�C�e�����擾
            if (listItem is ILoopItem loopItem)
            {
                endItem = loopItem.Pair;
            }
            else if (listItem is IIfItem ifItem)
            {
                endItem = ifItem.Pair;
            }
            else if (listItem is UniversalCommandItem universalItem)
            {
                endItem = universalItem.Pair;
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
        /// �A�C�e���̃C���f�b�N�X���擾
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

        /// <summary>
        /// If�n�R�}���h�̃C���X�^���X���쐬
        /// </summary>
        private static AutoTool.Command.Interface.ICommand CreateIfCommandInstance(AutoTool.Command.Interface.ICommand parent, ICommandListItem listItem)
        {
            AutoTool.Command.Interface.ICommand command = listItem switch
            {
                IfImageExistItem ifImageExistItem => new IfImageExistCommand(parent, ifImageExistItem, _serviceProvider),
                IfImageNotExistItem ifImageNotExistItem => new IfImageNotExistCommand(parent, ifImageNotExistItem, _serviceProvider),
                _ => throw new NotSupportedException($"���Ή���If�A�C�e��: {listItem.GetType().Name}")
            };
            
            // LineNumber��ݒ�
            command.LineNumber = listItem.LineNumber;
            return command;
        }
    }
}
