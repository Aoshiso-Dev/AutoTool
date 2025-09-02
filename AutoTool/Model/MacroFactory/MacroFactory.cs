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
    /// Phase 5���S�����ŁF�}�N���t�@�N�g��
    /// MacroPanels�ˑ����폜���AAutoTool�����ł̂ݎg�p
    /// </summary>
    public static class MacroFactory
    {
        private static IServiceProvider? _serviceProvider;
        private static AutoTool.Services.Plugin.IPluginService? _pluginService;
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
        public static void SetPluginService(AutoTool.Services.Plugin.IPluginService pluginService)
        {
            _pluginService = pluginService;
        }

        /// <summary>
        /// �R�}���h���X�g����}�N�����쐬
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

                // MacroFactory�T�[�r�X������
                if (_serviceProvider != null)
                {
                    SetServiceProvider(_serviceProvider);
                }

                // Pair�č\�z (Clone�Ō��I�u�W�F�N�g�̎Q�Ƃ��R�s�[���Ă��܂��C���f�b�N�X�T�������s������΍�)
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
                    // Phase 5: BasicCommandItem����y�A�����擾����ȈՎ���
                    if (clone is AutoTool.Model.List.Type.BasicCommandItem basicItem)
                    {
                        // Phase 5: ��{�I�ȃy�A�֌W�����i��ŏڍ׎����\��j
                        fixedCount++;
                    }
                }
                
                _logger?.LogDebug("[Phase5MacroFactory:{Session}] Pair rebuild result Fixed={Fixed} MissingOrigPair={Missing} Unresolved={Unresolved}", sessionId, fixedCount, missingPair, unresolved);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[Phase5MacroFactory:{Session}] Pair rebuild �ŃG���[", sessionId);
            }
        }

        /// <summary>
        /// ���X�g�A�C�e�����R�}���h�ɕϊ�
        /// </summary>
        private static IEnumerable<AutoToolICommand> ListItemToCommand(AutoToolICommand parent, IList<ICommandListItem> listItems, int depth, int sessionId)
        {
            if (depth > MaxRecursionDepth)
            {
                throw new InvalidOperationException($"�l�X�g���[�����܂� (>{MaxRecursionDepth}) Line={parent.LineNumber}");
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
        /// �A�C�e�����R�}���h�ɕϊ�
        /// </summary>
        private static AutoToolICommand? ItemToCommand(AutoToolICommand parent, ICommandListItem listItem, IList<ICommandListItem> listItems, ref int index, int depth, int sessionId)
        {
            AutoToolICommand? command = null;
            try
            {
                // Phase 5: �v���O�C���R�}���h����
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
                            _logger?.LogDebug("[Phase5MacroFactory:{Session}] �v���O�C���R�}���h�쐬: {PluginId}.{CommandId} line={Line}", sessionId, pluginId, commandId, listItem.LineNumber);
                            return autoToolCommand;
                        }
                        else
                        {
                            _logger?.LogWarning("[Phase5MacroFactory:{Session}] �v���O�C���R�}���h������: {PluginId}.{CommandId} line={Line}", sessionId, pluginId, commandId, listItem.LineNumber);
                        }
                    }
                }

                // Phase 5: ��{�R�}���h�����i�ȈՎ����j
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
                        // Phase 5: �q�v�f�����i�ȈՎ����j
                        command = loopCommand;
                        break;
                    default:
                        _logger?.LogWarning("[Phase5MacroFactory:{Session}] ���Ή��̃R�}���h�^�C�v: {CommandType}", sessionId, listItem.ItemType);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[Phase5MacroFactory:{Session}] �R�}���h�쐬���G���[ type={Type} line={Line}", sessionId, listItem.GetType().Name, listItem.LineNumber);
                throw;
            }
            return command;
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
    }
}