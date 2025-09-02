using System;
using System.Collections.Generic;
using System.Linq;
using MacroPanels.Model.List.Interface;
using Microsoft.Extensions.Logging;

namespace MacroPanels.Model.CommandDefinition
{
    /// <summary>
    /// CommandRegistry�̐ÓI�N���X��DI�Ή��ɂ���A�_�v�^�[
    /// </summary>
    public class CommandRegistryAdapter : ICommandRegistry
    {
        private readonly ILogger<CommandRegistryAdapter> _logger;

        public CommandRegistryAdapter(ILogger<CommandRegistryAdapter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("CommandRegistryAdapter�����������Ă��܂�");
            Initialize();
        }

        public void Initialize()
        {
            try
            {
                _logger.LogDebug("CommandRegistry�̏��������J�n���܂�");
                CommandRegistry.Initialize();
                _logger.LogDebug("CommandRegistry�̏��������������܂���");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CommandRegistry�̏��������ɃG���[���������܂���");
                throw;
            }
        }

        public IEnumerable<string> GetAllTypeNames()
        {
            try
            {
                var typeNames = CommandRegistry.GetAllTypeNames();
                _logger.LogDebug("���ׂẴ^�C�v�����擾: {Count}��", typeNames.Count());
                return typeNames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�^�C�v���擾���ɃG���[���������܂���");
                return Enumerable.Empty<string>();
            }
        }

        public IEnumerable<string> GetOrderedTypeNames()
        {
            try
            {
                var typeNames = CommandRegistry.GetOrderedTypeNames();
                _logger.LogDebug("�����t����ꂽ�^�C�v�����擾: {Count}��", typeNames.Count());
                return typeNames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�����t���^�C�v���擾���ɃG���[���������܂���");
                return Enumerable.Empty<string>();
            }
        }

        public IEnumerable<string> GetTypeNamesByCategory(CommandCategory category)
        {
            try
            {
                var typeNames = CommandRegistry.GetTypeNamesByCategory(category);
                _logger.LogDebug("�J�e�S���ʃ^�C�v�����擾: {Category}, {Count}��", category, typeNames.Count());
                return typeNames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�J�e�S���ʃ^�C�v���擾���ɃG���[���������܂���: {Category}", category);
                return Enumerable.Empty<string>();
            }
        }

        public IEnumerable<string> GetTypeNamesByDisplayPriority(int priority)
        {
            try
            {
                var typeNames = CommandRegistry.GetTypeNamesByDisplayPriority(priority);
                _logger.LogDebug("�D��x�ʃ^�C�v�����擾: {Priority}, {Count}��", priority, typeNames.Count());
                return typeNames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�D��x�ʃ^�C�v���擾���ɃG���[���������܂���: {Priority}", priority);
                return Enumerable.Empty<string>();
            }
        }

        public ICommandListItem? CreateCommandItem(string typeName)
        {
            try
            {
                var item = CommandRegistry.CreateCommandItem(typeName);
                if (item != null)
                {
                    _logger.LogDebug("�R�}���h�A�C�e�����쐬���܂���: {TypeName}", typeName);
                }
                else
                {
                    _logger.LogWarning("�R�}���h�A�C�e���̍쐬�Ɏ��s���܂���: {TypeName}", typeName);
                }
                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�R�}���h�A�C�e���쐬���ɃG���[���������܂���: {TypeName}", typeName);
                return null;
            }
        }

        public bool IsIfCommand(string typeName)
        {
            try
            {
                return CommandRegistry.IsIfCommand(typeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "If�R�}���h���蒆�ɃG���[���������܂���: {TypeName}", typeName);
                return false;
            }
        }

        public bool IsLoopCommand(string typeName)
        {
            try
            {
                return CommandRegistry.IsLoopCommand(typeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���[�v�R�}���h���蒆�ɃG���[���������܂���: {TypeName}", typeName);
                return false;
            }
        }

        public bool IsEndCommand(string typeName)
        {
            try
            {
                return CommandRegistry.IsEndCommand(typeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�I���R�}���h���蒆�ɃG���[���������܂���: {TypeName}", typeName);
                return false;
            }
        }

        public bool IsStartCommand(string typeName)
        {
            try
            {
                return CommandRegistry.IsStartCommand(typeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�J�n�R�}���h���蒆�ɃG���[���������܂���: {TypeName}", typeName);
                return false;
            }
        }

        public Type? GetItemType(string typeName)
        {
            try
            {
                return CommandRegistry.GetItemType(typeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�A�C�e���^�C�v�擾���ɃG���[���������܂���: {TypeName}", typeName);
                return null;
            }
        }

        public IEnumerable<CommandDefinitionItem> GetCommandDefinitions()
        {
            try
            {
                var definitions = GetOrderedTypeNames()
                    .Select(typeName => new CommandDefinitionItem
                    {
                        TypeName = typeName,
                        DisplayName = CommandRegistry.DisplayOrder.GetDisplayName(typeName),
                        Category = GetCategoryFromPriority(CommandRegistry.DisplayOrder.GetPriority(typeName)),
                        Description = CommandRegistry.DisplayOrder.GetDescription(typeName),
                        Priority = CommandRegistry.DisplayOrder.GetPriority(typeName),
                        SubPriority = CommandRegistry.DisplayOrder.GetSubPriority(typeName),
                        IsIfCommand = IsIfCommand(typeName),
                        IsLoopCommand = IsLoopCommand(typeName)
                    })
                    .ToList();

                _logger.LogDebug("�R�}���h��`�ꗗ���擾: {Count}��", definitions.Count);
                return definitions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�R�}���h��`�ꗗ�擾���ɃG���[���������܂���");
                return Enumerable.Empty<CommandDefinitionItem>();
            }
        }

        private static CommandCategory GetCategoryFromPriority(int priority)
        {
            return priority switch
            {
                1 => CommandCategory.Action,   // �N���b�N����
                2 => CommandCategory.Action,   // ��{����
                3 => CommandCategory.Control,  // ���[�v����
                4 => CommandCategory.Control,  // ��������
                5 => CommandCategory.Variable, // �ϐ�����
                6 => CommandCategory.System,   // �V�X�e������
                _ => CommandCategory.Action    // ���̑�
            };
        }
    }
}