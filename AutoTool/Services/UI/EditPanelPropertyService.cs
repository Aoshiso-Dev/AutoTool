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
    /// EditPanel用プロパティサービス（DirectCommandRegistry統合版）
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

            // メッセージ受信の設定
            _messenger.Register<ChangeSelectedMessage>(this, (r, m) =>
            {
                _currentItem = m.SelectedItem;
            });

            _logger.LogDebug("EditPanelPropertyService を初期化しました");
        }

        #region アイテムタイプ判定プロパティ

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

        #region プロパティ操作

        public T? GetProperty<T>(string propertyName, T? defaultValue = default)
        {
            if (_currentItem == null) return defaultValue;

            try
            {
                // UniversalCommandItemの場合は動的設定から取得
                if (_currentItem is UniversalCommandItem universalItem)
                {
                    return universalItem.GetSetting<T>(propertyName, defaultValue);
                }

                // 通常のプロパティから取得
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
                _logger.LogDebug("プロパティ取得エラー: {Property} - {Error}", propertyName, ex.Message);
            }

            return defaultValue;
        }

        public void SetProperty<T>(string propertyName, T value)
        {
            if (_currentItem == null) return;

            try
            {
                // UniversalCommandItemの場合は動的設定に保存
                if (_currentItem is UniversalCommandItem universalItem)
                {
                    universalItem.SetSetting(propertyName, value);
                    return;
                }

                // 通常のプロパティに設定
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
                _logger.LogError(ex, "プロパティ設定エラー: {Property} = {Value}", propertyName, value);
            }
        }

        #endregion

        #region 設定定義関連

        /// <summary>
        /// 指定されたコマンドの設定定義を取得
        /// </summary>
        public List<SettingDefinition> GetSettingDefinitions(string commandType)
        {
            try
            {
                _logger.LogDebug("設定定義取得開始: {CommandType}", commandType);
                
                var definitions = DirectCommandRegistry.GetSettingDefinitions(commandType);
                
                _logger.LogDebug("設定定義取得完了: {CommandType} -> {Count}項目", commandType, definitions.Count);
                return definitions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定定義取得中にエラー: {CommandType}", commandType);
                return new List<SettingDefinition>();
            }
        }

        /// <summary>
        /// コマンドアイテムの設定値を適用
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
                    // 通常のアイテムの場合はリフレクションで設定
                    foreach (var kvp in settings)
                    {
                        var property = item.GetType().GetProperty(kvp.Key);
                        if (property != null && property.CanWrite)
                        {
                            property.SetValue(item, kvp.Value);
                        }
                    }
                }
                
                _logger.LogDebug("設定値適用完了: {ItemType} - {SettingCount}項目", item.ItemType, settings.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定値適用中にエラー: {ItemType}", item.ItemType);
            }
        }

        /// <summary>
        /// コマンドアイテムから設定値を取得
        /// </summary>
        public Dictionary<string, object?> GetSettings(ICommandListItem item)
        {
            var settings = new Dictionary<string, object?>();
            
            try
            {
                if (item is UniversalCommandItem universalItem)
                {
                    // UniversalCommandItemの動的設定から取得
                    foreach (var kvp in universalItem.Settings)
                    {
                        settings[kvp.Key] = kvp.Value;
                    }
                }
                else
                {
                    // 通常のアイテムからリフレクションで取得
                    var properties = item.GetType().GetProperties()
                        .Where(p => p.CanRead && p.Name != "ItemType" && p.Name != "LineNumber");
                    
                    foreach (var property in properties)
                    {
                        var value = property.GetValue(item);
                        settings[property.Name] = value;
                    }
                }
                
                _logger.LogDebug("設定値取得完了: {ItemType} - {SettingCount}項目", item.ItemType, settings.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定値取得中にエラー: {ItemType}", item.ItemType);
            }
            
            return settings;
        }

        /// <summary>
        /// ソースコレクションを取得
        /// </summary>
        public object[]? GetSourceCollection(string collectionName)
        {
            try
            {
                return DirectCommandRegistry.GetSourceCollection(collectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ソースコレクション取得中にエラー: {CollectionName}", collectionName);
                return null;
            }
        }

        #endregion

        #region 追加メソッド（互換性用）

        /// <summary>
        /// 利用可能なコマンドタイプの一覧を取得
        /// </summary>
        public ObservableCollection<CommandDisplayItem> GetAvailableCommandTypes()
        {
            try
            {
                _logger.LogDebug("利用可能なコマンドタイプの取得を開始");

                // DirectCommandRegistryからコマンドタイプを取得
                var commandTypes = DirectCommandRegistry.GetOrderedTypeNames()
                    .Select(typeName => new CommandDisplayItem
                    {
                        TypeName = typeName,
                        DisplayName = DirectCommandRegistry.DisplayOrder.GetDisplayName(typeName),
                        Category = DirectCommandRegistry.DisplayOrder.GetCategoryName(typeName)
                    })
                    .ToList();

                _logger.LogDebug("コマンドタイプ取得完了: {Count}個", commandTypes.Count);
                return new ObservableCollection<CommandDisplayItem>(commandTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンドタイプ取得中にエラーが発生");
                return new ObservableCollection<CommandDisplayItem>();
            }
        }

        /// <summary>
        /// コマンドの表示名を取得
        /// </summary>
        public string GetCommandDisplayName(string commandId)
        {
            try
            {
                return DirectCommandRegistry.DisplayOrder.GetDisplayName(commandId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "コマンド表示名取得中にエラー: {CommandId}", commandId);
                return commandId;
            }
        }

        /// <summary>
        /// コマンドの説明を取得
        /// </summary>
        public string GetCommandDescription(string commandId)
        {
            try
            {
                return DirectCommandRegistry.DisplayOrder.GetDescription(commandId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "コマンド説明取得中にエラー: {CommandId}", commandId);
                return $"{commandId}コマンド";
            }
        }

        #endregion
    }
}