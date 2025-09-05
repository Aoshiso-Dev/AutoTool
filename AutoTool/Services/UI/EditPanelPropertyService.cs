using AutoTool.Command.Interface;
using AutoTool.Model.List.Interface;
using AutoTool.Model.CommandDefinition;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoTool.ViewModel.Shared;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows.Input;
using System.Windows.Media;
using AutoTool.Message;
using AutoTool.Model.List.Class;

namespace AutoTool.Services.UI
{
    /// <summary>
    /// EditPanel�p�v���p�e�B�T�[�r�X�iDirectCommandRegistry�����Łj
    /// </summary>
    public class EditPanelPropertyService : IEditPanelPropertyService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EditPanelPropertyService> _logger;
        private readonly IMessenger _messenger;
        private ICommandListItem? _currentItem;

        public EditPanelPropertyService(IServiceProvider serviceProvider, ILogger<EditPanelPropertyService> logger, IMessenger messenger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            // ���b�Z�[�W��M�̐ݒ�
            _messenger.Register<ChangeSelectedMessage>(this, (r, m) =>
            {
                _currentItem = m.SelectedItem;
            });

            _logger.LogDebug("EditPanelPropertyService �����������܂���");
        }

        #region �A�C�e���^�C�v����v���p�e�B

        public bool IsWaitImageItem => _currentItem?.ItemType == "Wait_Image";
        public bool IsClickImageItem => _currentItem?.ItemType == "Click_Image";
        public bool IsClickImageAIItem => _currentItem?.ItemType == "Click_Image_AI";
        public bool IsHotkeyItem => _currentItem?.ItemType == "Hotkey";
        public bool IsClickItem => _currentItem?.ItemType == "Click";
        public bool IsWaitItem => _currentItem?.ItemType == "Wait";
        public bool IsLoopItem => _currentItem?.ItemType == "Loop";
        public bool IsLoopEndItem => _currentItem?.ItemType == "Loop_End";
        public bool IsLoopBreakItem => _currentItem?.ItemType == "Loop_Break";
        public bool IsIfImageExistItem => _currentItem?.ItemType == "IF_ImageExist";
        public bool IsIfImageNotExistItem => _currentItem?.ItemType == "IF_ImageNotExist";
        public bool IsIfImageExistAIItem => _currentItem?.ItemType == "IF_ImageExist_AI";
        public bool IsIfImageNotExistAIItem => _currentItem?.ItemType == "IF_ImageNotExist_AI";
        public bool IsIfEndItem => _currentItem?.ItemType == "IF_End";
        public bool IsIfVariableItem => _currentItem?.ItemType == "IF_Variable";
        public bool IsExecuteItem => _currentItem?.ItemType == "Execute";
        public bool IsSetVariableItem => _currentItem?.ItemType == "SetVariable";
        public bool IsSetVariableAIItem => _currentItem?.ItemType == "SetVariable_AI";
        public bool IsScreenshotItem => _currentItem?.ItemType == "Screenshot";

        #endregion

        #region �v���p�e�B����

        public T? GetProperty<T>(string propertyName, T? defaultValue = default)
        {
            if (_currentItem == null) return defaultValue;

            try
            {
                // UniversalCommandItem�̏ꍇ�͓��I�ݒ肩��擾
                if (_currentItem is UniversalCommandItem universalItem)
                {
                    return universalItem.GetSetting<T>(propertyName, defaultValue);
                }

                // �ʏ�̃v���p�e�B����擾
                var property = _currentItem.GetType().GetProperty(propertyName);
                if (property != null)
                {
                    var value = property.GetValue(_currentItem);
                    if (value is T tValue)
                        return tValue;
                    if (value != null && typeof(T).IsAssignableFrom(value.GetType()))
                        return (T)value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("�v���p�e�B�擾�G���[: {Property} - {Error}", propertyName, ex.Message);
            }

            return defaultValue;
        }

        public void SetProperty<T>(string propertyName, T value)
        {
            if (_currentItem == null) return;

            try
            {
                // UniversalCommandItem�̏ꍇ�͓��I�ݒ�ɕۑ�
                if (_currentItem is UniversalCommandItem universalItem)
                {
                    universalItem.SetSetting(propertyName, value);
                    return;
                }

                // �ʏ�̃v���p�e�B�ɐݒ�
                var property = _currentItem.GetType().GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    var currentValue = property.GetValue(_currentItem);
                    if (!Equals(currentValue, value))
                    {
                        property.SetValue(_currentItem, value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�v���p�e�B�ݒ�G���[: {Property} = {Value}", propertyName, value);
            }
        }

        #endregion

        #region �ݒ��`�֘A

        /// <summary>
        /// �w�肳�ꂽ�R�}���h�̐ݒ��`���擾
        /// </summary>
        public List<SettingDefinition> GetSettingDefinitions(string commandType)
        {
            try
            {
                _logger.LogDebug("�ݒ��`�擾�J�n: {CommandType}", commandType);
                
                var definitions = DirectCommandRegistry.GetSettingDefinitions(commandType);
                
                _logger.LogDebug("�ݒ��`�擾����: {CommandType} -> {Count}����", commandType, definitions.Count);
                return definitions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ݒ��`�擾���ɃG���[: {CommandType}", commandType);
                return new List<SettingDefinition>();
            }
        }

        /// <summary>
        /// �R�}���h�A�C�e���̐ݒ�l��K�p
        /// </summary>
        public void ApplySettings(ICommandListItem item, Dictionary<string, object?> settings)
        {
            try
            {
                if (item is UniversalCommandItem universalItem)
                {
                    foreach (var kvp in settings)
                    {
                        universalItem.SetSetting(kvp.Key, kvp.Value);
                    }
                }
                else
                {
                    // �ʏ�̃A�C�e���̏ꍇ�̓��t���N�V�����Őݒ�
                    foreach (var kvp in settings)
                    {
                        var property = item.GetType().GetProperty(kvp.Key);
                        if (property != null && property.CanWrite)
                        {
                            property.SetValue(item, kvp.Value);
                        }
                    }
                }
                
                _logger.LogDebug("�ݒ�l�K�p����: {ItemType} - {SettingCount}����", item.ItemType, settings.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ݒ�l�K�p���ɃG���[: {ItemType}", item.ItemType);
            }
        }

        /// <summary>
        /// �R�}���h�A�C�e������ݒ�l���擾
        /// </summary>
        public Dictionary<string, object?> GetSettings(ICommandListItem item)
        {
            var settings = new Dictionary<string, object?>();
            
            try
            {
                if (item is UniversalCommandItem universalItem)
                {
                    // UniversalCommandItem�̓��I�ݒ肩��擾
                    foreach (var kvp in universalItem.Settings)
                    {
                        settings[kvp.Key] = kvp.Value;
                    }
                }
                else
                {
                    // �ʏ�̃A�C�e�����烊�t���N�V�����Ŏ擾
                    var properties = item.GetType().GetProperties()
                        .Where(p => p.CanRead && p.Name != "ItemType" && p.Name != "LineNumber");
                    
                    foreach (var property in properties)
                    {
                        var value = property.GetValue(item);
                        settings[property.Name] = value;
                    }
                }
                
                _logger.LogDebug("�ݒ�l�擾����: {ItemType} - {SettingCount}����", item.ItemType, settings.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ݒ�l�擾���ɃG���[: {ItemType}", item.ItemType);
            }
            
            return settings;
        }

        /// <summary>
        /// �\�[�X�R���N�V�������擾
        /// </summary>
        public object[]? GetSourceCollection(string collectionName)
        {
            try
            {
                return DirectCommandRegistry.GetSourceCollection(collectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�\�[�X�R���N�V�����擾���ɃG���[: {CollectionName}", collectionName);
                return null;
            }
        }

        #endregion

        #region �ǉ����\�b�h�i�݊����p�j

        /// <summary>
        /// ���p�\�ȃR�}���h�^�C�v�̈ꗗ���擾
        /// </summary>
        public ObservableCollection<CommandDisplayItem> GetAvailableCommandTypes()
        {
            try
            {
                _logger.LogDebug("���p�\�ȃR�}���h�^�C�v�̎擾���J�n");

                // DirectCommandRegistry����R�}���h�^�C�v���擾
                var commandTypes = DirectCommandRegistry.GetOrderedTypeNames()
                    .Select(typeName => new CommandDisplayItem
                    {
                        TypeName = typeName,
                        DisplayName = DirectCommandRegistry.DisplayOrder.GetDisplayName(typeName),
                        Category = DirectCommandRegistry.DisplayOrder.GetCategoryName(typeName)
                    })
                    .ToList();

                _logger.LogDebug("�R�}���h�^�C�v�擾����: {Count}��", commandTypes.Count);
                return new ObservableCollection<CommandDisplayItem>(commandTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�R�}���h�^�C�v�擾���ɃG���[������");
                return new ObservableCollection<CommandDisplayItem>();
            }
        }

        /// <summary>
        /// �R�}���h�̕\�������擾
        /// </summary>
        public string GetCommandDisplayName(string commandId)
        {
            try
            {
                return DirectCommandRegistry.DisplayOrder.GetDisplayName(commandId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "�R�}���h�\�����擾���ɃG���[: {CommandId}", commandId);
                return commandId;
            }
        }

        /// <summary>
        /// �R�}���h�̐������擾
        /// </summary>
        public string GetCommandDescription(string commandId)
        {
            try
            {
                return DirectCommandRegistry.DisplayOrder.GetDescription(commandId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "�R�}���h�����擾���ɃG���[: {CommandId}", commandId);
                return $"{commandId}�R�}���h";
            }
        }

        #endregion
    }
}