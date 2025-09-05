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
    /// �}�N���t�@�N�g���iDirectCommandRegistry�����Łj
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
                
                _logger?.LogDebug("[MacroFactory:{Session}] CreateMacro start Items={Count} CloneElapsed={CloneMs}ms", 
                    sessionId, cloneItems.Count, cloneSw.ElapsedMilliseconds);

                // Pair�č\�z (Clone�Ō��I�u�W�F�N�g�̎Q�Ƃ��R�s�[���Ă��܂��C���f�b�N�X�T�������s������΍�)
                var pairFixSw = Stopwatch.StartNew();
                RebuildPairRelationships(cloneItems, originalList, sessionId);
                pairFixSw.Stop();
                
                _logger?.LogDebug("[MacroFactory:{Session}] Pair rebuild elapsed={Ms}ms", sessionId, pairFixSw.ElapsedMilliseconds);

                // ���[�g�R�}���h�쐬
                var root = new RootCommand(_serviceProvider)
                {
                    LineNumber = 0
                };

                // �q�R�}���h���쐬
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
        /// �q�R�}���h���쐬
        /// </summary>
        private static IEnumerable<AutoTool.Command.Interface.ICommand> CreateChildCommands(
            AutoTool.Command.Interface.ICommand parent, 
            IList<ICommandListItem> listItems, 
            int depth, 
            int sessionId)
        {
            if (depth > MaxRecursionDepth)
            {
                throw new InvalidOperationException($"�l�X�g���x�����ߏ� (>{MaxRecursionDepth}) Line={parent.LineNumber}");
            }

            var commands = new List<AutoTool.Command.Interface.ICommand>();
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
        /// �A�C�e������R�}���h���쐬�iDirectCommandRegistry�����Łj
        /// </summary>
        private static AutoTool.Command.Interface.ICommand? CreateCommandFromItem(
            AutoTool.Command.Interface.ICommand parent, 
            ICommandListItem listItem, 
            IList<ICommandListItem> listItems, 
            ref int index, 
            int depth, 
            int sessionId)
        {
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
                            _logger?.LogDebug("[MacroFactory:{Session}] �v���O�C���R�}���h����: {PluginId}.{CommandId} line={Line}", 
                                sessionId, pluginId, commandId, listItem.LineNumber);
                            return autoToolCommand;
                        }
                    }
                }

                // 2. UniversalCommandItem�̏���
                UniversalCommandItem universalItem;
                if (listItem is UniversalCommandItem existing)
                {
                    universalItem = existing;
                }
                else
                {
                    // �Â��A�C�e����UniversalCommandItem�ɕϊ�
                    universalItem = ConvertToUniversalItem(listItem);
                }

                // 3. DirectCommandRegistry�ŃR�}���h�쐬
                var command = DirectCommandRegistry.CreateCommand(universalItem.ItemType, parent, universalItem, _serviceProvider!);
                if (command != null)
                {
                    command.LineNumber = listItem.LineNumber;
                    command.IsEnabled = listItem.IsEnable;
                    command.NestLevel = depth;

                    _logger?.LogTrace("[MacroFactory:{Session}] DirectCommandRegistry�ŃR�}���h����: {ItemType} -> {CommandType} line={Line}", 
                        sessionId, listItem.ItemType, command.GetType().Name, listItem.LineNumber);

                    // ���G�ȃR�}���h�iLoop�AIf�j�̎q�v�f����
                    if (IsComplexCommand(universalItem.ItemType))
                    {
                        ProcessComplexCommand(command, universalItem, listItems, ref index, depth, sessionId);
                    }

                    return command;
                }

                _logger?.LogWarning("[MacroFactory:{Session}] ���Ή��̃R�}���h�^�C�v: {ItemType} line={Line}", 
                    sessionId, listItem.ItemType, listItem.LineNumber);
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[MacroFactory:{Session}] �R�}���h�������ɃG���[���� type={Type} line={Line}", 
                    sessionId, listItem.GetType().Name, listItem.LineNumber);
                throw;
            }
        }

        /// <summary>
        /// ���G�ȃR�}���h�iLoop�AIf�j���ǂ����𔻒�
        /// </summary>
        private static bool IsComplexCommand(string itemType)
        {
            return itemType switch
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
        private static void ProcessComplexCommand(
            AutoTool.Command.Interface.ICommand command, 
            UniversalCommandItem item, 
            IList<ICommandListItem> listItems, 
            ref int index, 
            int depth, 
            int sessionId)
        {
            var endCommandType = GetEndCommandType(item.ItemType);
            if (string.IsNullOrEmpty(endCommandType))
            {
                return;
            }

            index++; // �J�n�R�}���h�̎����珈���J�n

            // �q�v�f������
            while (index < listItems.Count)
            {
                var childItem = listItems[index];

                // �I���R�}���h�ɒB������I��
                if (childItem.ItemType == endCommandType)
                {
                    break;
                }

                var childCommand = CreateCommandFromItem(command, childItem, listItems, ref index, depth + 1, sessionId);
                if (childCommand != null)
                {
                    command.AddChild(childCommand);
                }

                // �q�v�f�����G�ȃR�}���h�łȂ��ꍇ�̓C���f�b�N�X��i�߂�
                if (!IsComplexCommand(childItem.ItemType))
                {
                    index++;
                }
            }
        }

        /// <summary>
        /// �J�n�R�}���h�ɑΉ�����I���R�}���h�^�C�v���擾
        /// </summary>
        private static string GetEndCommandType(string startCommandType)
        {
            return startCommandType switch
            {
                "Loop" => "Loop_End",
                "IF_ImageExist" => "IF_End",
                "IF_ImageNotExist" => "IF_End",
                "IF_ImageExist_AI" => "IF_End",
                "IF_ImageNotExist_AI" => "IF_End",
                "IF_Variable" => "IF_End",
                _ => string.Empty
            };
        }

        /// <summary>
        /// ICommandListItem��UniversalCommandItem�ɕϊ�
        /// </summary>
        private static UniversalCommandItem ConvertToUniversalItem(ICommandListItem item)
        {
            if (item is UniversalCommandItem universalItem)
            {
                return universalItem;
            }

            // �Â��A�C�e������UniversalCommandItem���쐬
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

            // Pair�v���p�e�B�����݂���ꍇ�̓R�s�[
            var pairProperty = item.GetType().GetProperty("Pair");
            if (pairProperty != null)
            {
                var pairValue = pairProperty.GetValue(item) as ICommandListItem;
                newItem.Pair = pairValue;
            }

            // �ݒ��`��������
            newItem.InitializeSettingDefinitions();
            
            _logger?.LogTrace("�Â��A�C�e����UniversalCommandItem�ɕϊ�: {ItemType} line={Line}", 
                item.ItemType, item.LineNumber);

            return newItem;
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
                    if (pairProp == null) continue;

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
                        continue;
                    }

                    if (!lineToClone.TryGetValue(origPair.LineNumber, out var clonePair))
                    {
                        unresolved++;
                        continue;
                    }

                    // ���ɐ������Ȃ�X�L�b�v
                    var currentPair = pairProp.GetValue(clone) as ICommandListItem;
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
                _logger?.LogWarning(ex, "[MacroFactory:{Session}] Pair rebuild �ŃG���[", sessionId);
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
                    foreach (var cmd in invalidCommands.Take(5))
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
    }
}
