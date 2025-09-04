using AutoTool.Model.CommandDefinition;
using AutoTool.Model.List.Class;
using AutoTool.Command.Class;
using AutoTool.Command.Interface;
using AutoTool.List.Class;
using AutoTool.Model.List.Interface;
using AutoTool.Model.List.Type;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AutoTool.Model.MacroFactory;
using AutoTool.Services.Configuration;
using AutoTool.Helpers;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace AutoTool.List.Class
{
    /// <summary>
    /// �R�}���h���X�g�̊Ǘ��T�[�r�X�iILogger�Ή��Łj
    /// UI�o�C���f�B���O�͕s�v�Ȃ��߁AObservableObject�͌p�����Ȃ�
    /// </summary>
    public class CommandListService
    {
        private readonly ILogger<CommandListService> _logger;
        private readonly ObservableCollection<ICommandListItem> _items = new();

        /// <summary>
        /// �A�C�e���R���N�V�����i�ǂݎ���p�v���p�e�B�j
        /// </summary>
        public ObservableCollection<ICommandListItem> Items => _items;

        /// <summary>
        /// �C���f�N�T
        /// </summary>
        public ICommandListItem this[int index]
        {
            get => _items[index];
            set => _items[index] = value;
        }

        /// <summary>
        /// �R���X�g���N�^�iILogger�����j
        /// </summary>
        public CommandListService(ILogger<CommandListService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogDebug("CommandListService����������");
        }

        /// <summary>
        /// ���X�g�ύX��̋��ʏ���
        /// </summary>
        private void RefreshListState()
        {
            try
            {
                _logger.LogTrace("���X�g��ԍX�V�J�n");
                ReorderItems();
                CalculateNestLevel();
                PairIfItems();
                PairLoopItems();
                _logger.LogTrace("���X�g��ԍX�V����");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���X�g��ԍX�V���ɃG���[���������܂���");
                throw;
            }
        }

        #region �R���N�V�������상�\�b�h

        public void Add(ICommandListItem item)
        {
            ExecuteOnUIThread(() =>
            {
                _items.Add(item);
                RefreshListState();
                _logger.LogDebug("�A�C�e���ǉ�: {ItemType} (����: {Count})", item.ItemType, _items.Count);
            });
        }

        public void Remove(ICommandListItem item)
        {
            ExecuteOnUIThread(() =>
            {
                _items.Remove(item);
                RefreshListState();
                _logger.LogDebug("�A�C�e���폜: {ItemType} (����: {Count})", item.ItemType, _items.Count);
            });
        }

        public void RemoveAt(int index)
        {
            ExecuteOnUIThread(() =>
            {
                if (index >= 0 && index < _items.Count)
                {
                    var item = _items[index];
                    _items.RemoveAt(index);
                    RefreshListState();
                    _logger.LogDebug("�C���f�b�N�X {Index} �̃A�C�e���폜: {ItemType} (����: {Count})", 
                        index, item.ItemType, _items.Count);
                }
                else
                {
                    _logger.LogWarning("�����ȃC���f�b�N�X�ł̍폜���s: {Index} (�L���͈�: 0-{MaxIndex})", 
                        index, _items.Count - 1);
                }
            });
        }

        public void Insert(int index, ICommandListItem item)
        {
            ExecuteOnUIThread(() =>
            {
                _items.Insert(index, item);
                RefreshListState();
                _logger.LogDebug("�A�C�e���}��: {ItemType} at {Index} (����: {Count})", 
                    item.ItemType, index, _items.Count);
            });
        }

        public void Override(int index, ICommandListItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            ExecuteOnUIThread(() =>
            {
                if (index < 0 || index >= _items.Count)
                {
                    _logger.LogError("�C���f�b�N�X�͈͊O: {Index} (�L���͈�: 0-{MaxIndex})", 
                        index, _items.Count - 1);
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                var oldItem = _items[index];
                _items[index] = item;
                RefreshListState();
                _logger.LogDebug("�A�C�e���u��: {OldType} -> {NewType} at {Index}", 
                    oldItem.ItemType, item.ItemType, index);
            });
        }

        public void Clear()
        {
            ExecuteOnUIThread(() =>
            {
                var count = _items.Count;
                _items.Clear();
                _logger.LogInformation("�S�A�C�e���N���A: {Count}���폜", count);
            });
        }

        public void Move(int oldIndex, int newIndex)
        {
            ExecuteOnUIThread(() =>
            {
                if (oldIndex < 0 || oldIndex >= _items.Count || newIndex < 0 || newIndex >= _items.Count)
                {
                    _logger.LogWarning("�����ȃC���f�b�N�X�ł̈ړ����s: {OldIndex} -> {NewIndex} (�L���͈�: 0-{MaxIndex})", 
                        oldIndex, newIndex, _items.Count - 1);
                    return;
                }

                var item = _items[oldIndex];
                _items.RemoveAt(oldIndex);
                _items.Insert(newIndex, item);
                RefreshListState();
                _logger.LogDebug("�A�C�e���ړ�: {ItemType} from {OldIndex} to {NewIndex}", 
                    item.ItemType, oldIndex, newIndex);
            });
        }

        public void Copy(int oldIndex, int newIndex)
        {
            ExecuteOnUIThread(() =>
            {
                if (oldIndex < 0 || oldIndex >= _items.Count || newIndex < 0 || newIndex >= _items.Count)
                {
                    _logger.LogWarning("�����ȃC���f�b�N�X�ł̃R�s�[���s: {OldIndex} -> {NewIndex} (�L���͈�: 0-{MaxIndex})", 
                        oldIndex, newIndex, _items.Count - 1);
                    return;
                }

                var item = _items[oldIndex];
                _items.Insert(newIndex, item);
                RefreshListState();
                _logger.LogDebug("�A�C�e���R�s�[: {ItemType} from {OldIndex} to {NewIndex}", 
                    item.ItemType, oldIndex, newIndex);
            });
        }

        #endregion

        #region ���X�g�������\�b�h

        public void ReorderItems()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].LineNumber = i + 1;
            }
            _logger.LogTrace("�s�ԍ��Ĕz�񊮗�: {Count}��", _items.Count);
        }

        public void CalculateNestLevel()
        {
            var nestLevel = 0;

            foreach (var item in _items)
            {
                // �l�X�g���x�������炷�R�}���h�i�I���n�j
                if (CommandRegistry.IsEndCommand(item.ItemType))
                {
                    nestLevel--;
                }

                item.NestLevel = nestLevel;

                // �l�X�g���x���𑝂₷�R�}���h�i�J�n�n�j
                if (CommandRegistry.IsStartCommand(item.ItemType))
                {
                    nestLevel++;
                }
            }
            _logger.LogTrace("�l�X�g���x���v�Z����: �ő僌�x�� {MaxLevel}", nestLevel);
        }

        private void PairItems<TStart, TEnd>(Func<ICommandListItem, bool> startPredicate, Func<ICommandListItem, bool> endPredicate)
            where TStart : class
            where TEnd : class
        {
            var startItems = _items.OfType<TStart>().Cast<ICommandListItem>()
                .Where(startPredicate)
                .OrderBy(x => x.LineNumber)
                .ToList();

            var endItems = _items.OfType<TEnd>().Cast<ICommandListItem>()
                .Where(endPredicate)
                .OrderBy(x => x.LineNumber)
                .ToList();

            var pairedCount = 0;
            foreach (var startItem in startItems)
            {
                var startPairItem = startItem as dynamic;
                if (startPairItem?.Pair != null) continue;

                foreach (var endItem in endItems)
                {
                    var endPairItem = endItem as dynamic;
                    if (endPairItem?.Pair != null) continue;

                    if (endItem.NestLevel == startItem.NestLevel && endItem.LineNumber > startItem.LineNumber)
                    {
                        startPairItem.Pair = endItem;
                        endPairItem.Pair = startItem;
                        pairedCount++;
                        break;
                    }
                }
            }
            _logger.LogTrace("�y�A�����O����: {StartType}-{EndType} {PairedCount}�g", 
                typeof(TStart).Name, typeof(TEnd).Name, pairedCount);
        }

        public void PairIfItems()
        {
            try
            {
                PairItems<AutoTool.Model.List.Interface.IIfItem, AutoTool.Model.List.Interface.IIfEndItem>(
                    x => CommandRegistry.IsIfCommand(x.ItemType),
                    x => x.ItemType == CommandRegistry.CommandTypes.IfEnd
                );
                _logger.LogTrace("If�A�C�e���y�A�����O����");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "If�A�C�e���y�A�����O���ɃG���[���������܂���");
            }
        }

        public void PairLoopItems()
        {
            try
            {
                PairItems<AutoTool.Model.List.Interface.ILoopItem, AutoTool.Model.List.Interface.ILoopEndItem>(
                    x => CommandRegistry.IsLoopCommand(x.ItemType),
                    x => x.ItemType == CommandRegistry.CommandTypes.LoopEnd
                );
                _logger.LogTrace("Loop�A�C�e���y�A�����O����");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loop�A�C�e���y�A�����O���ɃG���[���������܂���");
            }
        }

        #endregion

        #region �t�@�C�����상�\�b�h

        public IEnumerable<ICommandListItem> Clone()
        {
            var clone = new List<ICommandListItem>();

            foreach (var item in _items)
            {
                clone.Add(item.Clone());
            }

            _logger.LogDebug("���X�g�N���[���쐬: {Count}��", clone.Count);
            return clone;
        }

        public void Save(string filePath)
        {
            try
            {
                _logger.LogInformation("�t�@�C���ۑ��J�n: {FilePath}", filePath);
                var cloneItems = Clone().ToList();
                JsonSerializerHelper.SerializeToFile(cloneItems, filePath);
                _logger.LogInformation("�t�@�C���ۑ�����: {FilePath} ({Count}��)", filePath, cloneItems.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�t�@�C���ۑ��Ɏ��s���܂���: {FilePath}", filePath);
                throw new InvalidOperationException($"�t�@�C���ۑ��Ɏ��s���܂���: {ex.Message}", ex);
            }
        }

        public void Load(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogError("�t�@�C����������܂���: {FilePath}", filePath);
                    throw new FileNotFoundException($"�t�@�C����������܂���: {filePath}");
                }

                _logger.LogInformation("�t�@�C���ǂݍ��݊J�n: {FilePath}", filePath);

                var jsonContent = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    _logger.LogWarning("��̃t�@�C���ł�: {FilePath}", filePath);
                    ExecuteOnUIThread(() => _items.Clear());
                    return;
                }

                _logger.LogDebug("JSON���e�T�C�Y: {Size}����", jsonContent.Length);

                List<ICommandListItem>? deserializedItems = null;

                try
                {
                    using var doc = JsonDocument.Parse(jsonContent);
                    var root = doc.RootElement;

                    if (root.ValueKind == JsonValueKind.Object && 
                        root.TryGetProperty("$values", out var valuesElement) &&
                        valuesElement.ValueKind == JsonValueKind.Array)
                    {
                        _logger.LogDebug("�Q�ƕێ��`���Ƃ��ď���");
                        deserializedItems = ProcessReferencePreservationFormat(valuesElement);
                    }
                    else if (root.ValueKind == JsonValueKind.Array)
                    {
                        _logger.LogDebug("�ʏ�z��`���Ƃ��ď���");
                        deserializedItems = ProcessNormalArrayFormat(root);
                    }
                    else
                    {
                        _logger.LogDebug("�W���f�V���A���C�[�[�V���������s");
                        deserializedItems = JsonSerializerHelper.DeserializeFromFile<List<ICommandListItem>>(filePath);
                    }
                }
                catch (Exception parseEx)
                {
                    _logger.LogError(parseEx, "JSON��̓G���[");
                    
                    try
                    {
                        deserializedItems = JsonSerializerHelper.DeserializeFromFile<List<ICommandListItem>>(filePath);
                        _logger.LogInformation("�t�H�[���o�b�N�ŏ�������: {Count}��", deserializedItems?.Count ?? 0);
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "�t�H�[���o�b�N���s");
                        throw new InvalidDataException($"JSON�t�@�C���̉�͂Ɏ��s���܂���: {parseEx.Message}", parseEx);
                    }
                }

                if (deserializedItems != null && deserializedItems.Count > 0)
                {
                    _logger.LogDebug("UI�X���b�h�ŃA�C�e���ǉ��J�n: {Count}��", deserializedItems.Count);
                    
                    ExecuteOnUIThread(() =>
                    {
                        try
                        {
                            _items.Clear();

                            foreach (var item in deserializedItems)
                            {
                                if (item != null)
                                {
                                    ValidateAndRepairItem(item);
                                    _items.Add(item);
                                    _logger.LogTrace("�A�C�e���ǉ�: {ItemType} - {Comment}", item.ItemType, item.Comment);
                                }
                            }

                            RefreshListState();
                            _logger.LogInformation("�t�@�C���ǂݍ��݊���: {FilePath} ({Count}�̃A�C�e��)", filePath, _items.Count);
                        }
                        catch (Exception uiEx)
                        {
                            _logger.LogError(uiEx, "UI�X���b�h�������ɃG���[");
                            throw;
                        }
                    });
                }
                else
                {
                    _logger.LogWarning("�L���ȃA�C�e����������܂���ł���: {FilePath}", filePath);
                    ExecuteOnUIThread(() => _items.Clear());
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON�t�@�C���`���G���[: {FilePath}", filePath);
                throw new InvalidDataException($"JSON�t�@�C���̌`���������ł�: {jsonEx.Message}", jsonEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�t�@�C���ǂݍ��݃G���[: {FilePath}", filePath);
                throw new InvalidOperationException($"�t�@�C���ǂݍ��݂Ɏ��s���܂���: {ex.Message}", ex);
            }
        }

        #endregion

        #region JSON�f�V���A���C�[�[�V����

        private List<ICommandListItem> ProcessReferencePreservationFormat(JsonElement valuesElement)
        {
            try
            {
                _logger.LogDebug("�Q�ƕێ��`�������J�n");
                
                var items = new List<ICommandListItem>();
                var referenceMap = new Dictionary<string, ICommandListItem>();

                var arrayLength = valuesElement.GetArrayLength();
                _logger.LogDebug("�z��v�f��: {Count}", arrayLength);

                // ��1�p�X: �A�C�e���쐬
                int elementIndex = 0;
                foreach (var element in valuesElement.EnumerateArray())
                {
                    try
                    {
                        var item = CreateCommandItemFromElement(element);
                        if (item != null)
                        {
                            items.Add(item);
                            _logger.LogTrace("�A�C�e���쐬����[{Index}]: {ItemType}", elementIndex, item.ItemType);
                            
                            if (element.TryGetProperty("$id", out var idElement))
                            {
                                var id = idElement.GetString();
                                if (!string.IsNullOrEmpty(id))
                                {
                                    referenceMap[id] = item;
                                }
                            }
                        }
                        elementIndex++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "�v�f[{Index}]�������ɃG���[", elementIndex);
                        elementIndex++;
                    }
                }

                // ��2�p�X: �Q�Ɖ���
                for (int i = 0; i < items.Count; i++)
                {
                    try
                    {
                        var element = valuesElement.EnumerateArray().ElementAt(i);
                        ResolvePairReferences(items[i], element, referenceMap);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "�Q�Ɖ���[{Index}]�ŃG���[", i);
                    }
                }

                _logger.LogDebug("�Q�ƕێ��`����������: {Count}��", items.Count);
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�Q�ƕێ��`�������őS�̃G���[");
                throw;
            }
        }

        private List<ICommandListItem> ProcessNormalArrayFormat(JsonElement arrayElement)
        {
            var items = new List<ICommandListItem>();

            foreach (var element in arrayElement.EnumerateArray())
            {
                var item = CreateCommandItemFromElement(element);
                if (item != null)
                {
                    items.Add(item);
                }
            }

            _logger.LogDebug("�ʏ�z��`����������: {Count}��", items.Count);
            return items;
        }

        private ICommandListItem? CreateCommandItemFromElement(JsonElement element)
        {
            try
            {
                string? itemType = GetPropertyValue<string>(element, "ItemType", "itemType");
                
                if (string.IsNullOrEmpty(itemType))
                {
                    itemType = InferItemTypeFromProperties(element);
                    _logger.LogTrace("ItemType����: {ItemType}", itemType);
                }
                else
                {
                    _logger.LogTrace("ItemType�擾: {ItemType}", itemType);
                }

                ICommandListItem? item = null;
                if (!string.IsNullOrEmpty(itemType))
                {
                    try
                    {
                        item = CommandRegistry.CreateCommandItem(itemType);
                        if (item != null)
                        {
                            _logger.LogTrace("CommandRegistry�쐬����: {ActualType}", item.GetType().Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "CommandRegistry�쐬�G���[ for {ItemType}", itemType);
                    }
                }

                if (item == null)
                {
                    item = new BasicCommandItem();
                    item.ItemType = itemType ?? "Unknown";
                    _logger.LogDebug("BasicCommandItem�t�H�[���o�b�N: {ItemType}", item.ItemType);
                }

                RestorePropertiesFromElement(item, element);
                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�A�C�e���쐬���ɃG���[");
                return null;
            }
        }

        #endregion

        #region �w���p�[���\�b�h

        private static string InferItemTypeFromProperties(JsonElement element)
        {
            if (HasProperty(element, "LoopCount", "loopCount")) return "Loop";
            
            if (HasProperty(element, "ImagePath", "imagePath"))
            {
                if (HasProperty(element, "ModelPath", "modelPath")) return "Click_Image_AI";
                if (HasProperty(element, "Timeout", "timeout")) return "Wait_Image";
                return "Click_Image";
            }
            
            if (HasProperty(element, "Wait", "wait")) return "Wait";
            if (HasProperty(element, "X", "x") && HasProperty(element, "Y", "y")) return "Click";
            
            if (HasProperty(element, "Key", "key") && 
                (HasProperty(element, "Ctrl", "ctrl") || HasProperty(element, "Alt", "alt") || HasProperty(element, "Shift", "shift")))
                return "Hotkey";
            
            if (HasProperty(element, "Pair", "pair"))
            {
                var desc = GetPropertyValue<string>(element, "Description", "description") ?? "";
                if (desc.Contains("->") || desc.Contains("End"))
                {
                    return desc.Contains("Loop") || desc.Contains("���[�v") ? "Loop_End" : "IF_End";
                }
            }

            return "Unknown";
        }

        private static void RestorePropertiesFromElement(ICommandListItem item, JsonElement element)
        {
            item.Comment = GetPropertyValue<string>(element, "Comment", "comment") ?? string.Empty;
            item.Description = GetPropertyValue<string>(element, "Description", "description") ?? string.Empty;
            item.IsEnable = GetPropertyValue<bool>(element, "IsEnable", "isEnable");
            item.LineNumber = GetPropertyValue<int>(element, "LineNumber", "lineNumber");
            item.NestLevel = GetPropertyValue<int>(element, "NestLevel", "nestLevel");
            item.IsInLoop = GetPropertyValue<bool>(element, "IsInLoop", "isInLoop");
            item.IsInIf = GetPropertyValue<bool>(element, "IsInIf", "isInIf");
            item.Progress = GetPropertyValue<int>(element, "Progress", "progress");

            RestoreTypeSpecificPropertiesFromElement(item, element);
        }

        private static void RestoreTypeSpecificPropertiesFromElement(ICommandListItem item, JsonElement element)
        {
            var itemType = item.GetType();

            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.StartsWith("$")) continue;

                var propInfo = FindProperty(itemType, property.Name);
                if (propInfo != null && propInfo.CanWrite)
                {
                    try
                    {
                        var value = ConvertJsonValueToPropertyType(property.Value, propInfo.PropertyType);
                        if (value != null)
                        {
                            propInfo.SetValue(item, value);
                        }
                    }
                    catch
                    {
                        // �v���p�e�B�ݒ莸�s�͖���
                    }
                }
            }
        }

        private static void ResolvePairReferences(ICommandListItem item, JsonElement element, Dictionary<string, ICommandListItem> referenceMap)
        {
            if (element.TryGetProperty("Pair", out var pairElement) && 
                pairElement.ValueKind == JsonValueKind.Object &&
                pairElement.TryGetProperty("$ref", out var refElement))
            {
                var refId = refElement.GetString();
                if (!string.IsNullOrEmpty(refId) && referenceMap.TryGetValue(refId, out var pairItem))
                {
                    var pairProperty = item.GetType().GetProperty("Pair");
                    if (pairProperty != null && pairProperty.CanWrite)
                    {
                        pairProperty.SetValue(item, pairItem);
                    }
                }
            }
        }

        private static void ValidateAndRepairItem(ICommandListItem item)
        {
            if (string.IsNullOrEmpty(item.ItemType))
            {
                item.ItemType = item.GetType().Name.Replace("Item", "");
            }
            
            if (string.IsNullOrEmpty(item.Description))
            {
                item.Description = $"{item.ItemType}�R�}���h";
            }
        }

        private static T GetPropertyValue<T>(JsonElement element, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var propElement))
                {
                    return ConvertJsonValueToType<T>(propElement);
                }
            }
            return default(T)!;
        }

        private static bool HasProperty(JsonElement element, params string[] propertyNames)
        {
            return propertyNames.Any(name => element.TryGetProperty(name, out _));
        }

        private static T ConvertJsonValueToType<T>(JsonElement element)
        {
            try
            {
                var targetType = typeof(T);
                return (T)ConvertJsonValueToPropertyType(element, targetType)!;
            }
            catch
            {
                return default(T)!;
            }
        }

        private static object? ConvertJsonValueToPropertyType(JsonElement element, Type targetType)
        {
            try
            {
                var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

                if (element.ValueKind == JsonValueKind.Null) return null;
                if (underlyingType == typeof(string)) return element.GetString();
                
                if (underlyingType == typeof(int))
                    return element.ValueKind == JsonValueKind.Number ? element.GetInt32() : 
                           element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var i) ? i : 0;
                
                if (underlyingType == typeof(double))
                    return element.ValueKind == JsonValueKind.Number ? element.GetDouble() : 
                           element.ValueKind == JsonValueKind.String && double.TryParse(element.GetString(), out var d) ? d : 0.0;
                
                if (underlyingType == typeof(bool))
                    return element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False ? element.GetBoolean() :
                           element.ValueKind == JsonValueKind.String && bool.TryParse(element.GetString(), out var b) ? b : false;

                if (underlyingType.IsEnum)
                {
                    var stringValue = element.GetString();
                    return !string.IsNullOrEmpty(stringValue) && Enum.TryParse(underlyingType, stringValue, true, out var enumValue) ? 
                           enumValue : Enum.GetValues(underlyingType).GetValue(0);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static PropertyInfo? FindProperty(Type type, string propertyName)
        {
            return type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) ??
                   type.GetProperty(char.ToUpper(propertyName[0]) + propertyName.Substring(1));
        }

        private void ExecuteOnUIThread(Action action)
        {
            try
            {
                var app = System.Windows.Application.Current;
                if (app != null)
                {
                    if (app.Dispatcher.CheckAccess())
                    {
                        action();
                    }
                    else
                    {
                        app.Dispatcher.Invoke(action);
                    }
                }
                else
                {
                    _logger.LogWarning("Application.Current��null�̂��߁A���ڎ��s���܂�");
                    action();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UI�X���b�h���s���ɃG���[");
                throw new InvalidOperationException($"UI�X���b�h�ł̎��s�Ɏ��s���܂���: {ex.Message}", ex);
            }
        }

        #endregion
    }
}
