using System.Collections.ObjectModel;
using AutoTool.Model.List.Interface;
using AutoTool.Model.CommandDefinition;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTool.Model.List.Class
{
    /// <summary>
    /// �R�}���h���X�g�̊Ǘ��T�[�r�X�iDirectCommandRegistry�Ή��j
    /// UI �o�C���f�B���O�͕s�v�Ȃ��߁AObservableObject �͌p�����Ȃ�
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
        /// �R���X�g���N�^�iILogger �ˑ��j
        /// </summary>
        public CommandListService(ILogger<CommandListService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogDebug("CommandListService ������������܂���");
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
            if (item == null) throw new ArgumentNullException(nameof(item));

            _items.Add(item);
            RefreshListState();
            _logger.LogDebug("�A�C�e���ǉ�: {ItemType} (�s {LineNumber})", item.ItemType, item.LineNumber);
        }

        public void Insert(int index, ICommandListItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            _items.Insert(index, item);
            RefreshListState();
            _logger.LogDebug("�A�C�e���}��: {ItemType} (�C���f�b�N�X {Index})", item.ItemType, index);
        }

        public bool Remove(ICommandListItem item)
        {
            if (item == null) return false;

            var result = _items.Remove(item);
            if (result)
            {
                RefreshListState();
                _logger.LogDebug("�A�C�e���폜: {ItemType} (�s {LineNumber})", item.ItemType, item.LineNumber);
            }
            return result;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _items.Count) return;

            var item = _items[index];
            _items.RemoveAt(index);
            RefreshListState();
            _logger.LogDebug("�A�C�e���폜�i�C���f�b�N�X�j: {ItemType} (�C���f�b�N�X {Index})", item.ItemType, index);
        }

        public void Clear()
        {
            _items.Clear();
            _logger.LogDebug("�S�A�C�e���N���A");
        }

        public int IndexOf(ICommandListItem item)
        {
            return _items.IndexOf(item);
        }

        public bool Contains(ICommandListItem item)
        {
            return _items.Contains(item);
        }

        public int Count => _items.Count;

        #endregion

        #region ���X�g��ԊǗ�

        /// <summary>
        /// �A�C�e���̍s�ԍ����Čv�Z
        /// </summary>
        private void ReorderItems()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].LineNumber = i + 1;
            }
        }

        /// <summary>
        /// �l�X�g���x�����v�Z
        /// </summary>
        private void CalculateNestLevel()
        {
            int currentNestLevel = 0;

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];

                // �I���R�}���h�̏ꍇ�A��Ƀl�X�g���x����������
                if (DirectCommandRegistry.IsEndCommand(item.ItemType))
                {
                    currentNestLevel = Math.Max(0, currentNestLevel - 1);
                }

                item.NestLevel = currentNestLevel;

                // �J�n�R�}���h�̏ꍇ�A���̃A�C�e������l�X�g���x�����グ��
                if (DirectCommandRegistry.IsStartCommand(item.ItemType))
                {
                    currentNestLevel++;
                }
            }
        }

        /// <summary>
        /// If�n�A�C�e���̃y�A�����O
        /// </summary>
        private void PairIfItems()
        {
            try
            {
                PairItemsByType(
                    x => DirectCommandRegistry.IsIfCommand(x.ItemType),
                    x => x.ItemType == DirectCommandRegistry.CommandTypes.IfEnd
                );
                _logger.LogTrace("If�n�A�C�e���̃y�A�����O����");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "If�n�A�C�e���̃y�A�����O���ɃG���[");
                throw;
            }
        }

        /// <summary>
        /// Loop�n�A�C�e���̃y�A�����O
        /// </summary>
        private void PairLoopItems()
        {
            try
            {
                PairItemsByType(
                    x => DirectCommandRegistry.IsLoopCommand(x.ItemType),
                    x => x.ItemType == DirectCommandRegistry.CommandTypes.LoopEnd
                );
                _logger.LogTrace("Loop�n�A�C�e���̃y�A�����O����");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loop�n�A�C�e���̃y�A�����O���ɃG���[");
                throw;
            }
        }

        /// <summary>
        /// �w������ŃA�C�e�����y�A�����O
        /// </summary>
        private void PairItemsByType(Func<ICommandListItem, bool> startCondition, Func<ICommandListItem, bool> endCondition)
        {
            var stack = new Stack<ICommandListItem>();

            foreach (var item in _items)
            {
                if (startCondition(item))
                {
                    stack.Push(item);
                }
                else if (endCondition(item))
                {
                    if (stack.Count > 0)
                    {
                        var startItem = stack.Pop();
                        
                        // Pair�v���p�e�B�̐ݒ�i���t���N�V�������g�p�j
                        SetPairProperty(startItem, item);
                        SetPairProperty(item, startItem);
                    }
                }
            }
        }

        /// <summary>
        /// Pair�v���p�e�B��ݒ�i���t���N�V�����g�p�j
        /// </summary>
        private void SetPairProperty(ICommandListItem item, ICommandListItem pairItem)
        {
            try
            {
                var pairProperty = item.GetType().GetProperty("Pair");
                if (pairProperty != null && pairProperty.CanWrite)
                {
                    pairProperty.SetValue(item, pairItem);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Pair�v���p�e�B�̐ݒ�Ɏ��s: {ItemType}", item.ItemType);
            }
        }

        #endregion

        #region �t�@�C������

        /// <summary>
        /// JSON�t�@�C������R�}���h���X�g��ǂݍ���
        /// </summary>
        public async Task LoadFromFileAsync(string filePath)
        {
            try
            {
                _logger.LogInformation("�t�@�C���ǂݍ��݊J�n: {FilePath}", filePath);

                var jsonContent = await System.IO.File.ReadAllTextAsync(filePath);
                var loadedItems = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonContent);

                if (loadedItems == null)
                {
                    throw new InvalidOperationException("�t�@�C�����e�������ł�");
                }

                _items.Clear();

                foreach (var itemData in loadedItems)
                {
                    var item = CreateItemFromData(itemData);
                    if (item != null)
                    {
                        _items.Add(item);
                    }
                }

                RefreshListState();
                _logger.LogInformation("�t�@�C���ǂݍ��݊���: {Count}�̃A�C�e��", _items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�t�@�C���ǂݍ��ݒ��ɃG���[: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// �f�[�^����A�C�e�����쐬
        /// </summary>
        private ICommandListItem? CreateItemFromData(Dictionary<string, object> itemData)
        {
            try
            {
                if (!itemData.TryGetValue("ItemType", out var itemTypeObj) || itemTypeObj is not string itemType)
                {
                    return null;
                }

                // UniversalCommandItem�Ƃ��č쐬�����s
                if (itemData.TryGetValue("Settings", out var settingsObj) && settingsObj is System.Text.Json.JsonElement settingsElement)
                {
                    var universalItem = DirectCommandRegistry.CreateUniversalItem(itemType);
                    if (universalItem != null)
                    {
                        // �ݒ�l�𕜌�
                        RestoreSettings(universalItem, settingsElement);
                        RestoreBasicProperties(universalItem, itemData);
                        return universalItem;
                    }
                }

                // �t�H�[���o�b�N: BasicCommandItem
                var basicItem = new AutoTool.Model.List.Type.BasicCommandItem();
                basicItem.ItemType = itemType;
                RestoreBasicProperties(basicItem, itemData);
                return basicItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�A�C�e���쐬���ɃG���[");
                return null;
            }
        }

        private void RestoreSettings(UniversalCommandItem item, System.Text.Json.JsonElement settingsElement)
        {
            try
            {
                var settingsDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(settingsElement.GetRawText());
                if (settingsDict != null)
                {
                    foreach (var kvp in settingsDict)
                    {
                        item.SetSetting(kvp.Key, kvp.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "�ݒ�l�������ɃG���[");
            }
        }

        private void RestoreBasicProperties(ICommandListItem item, Dictionary<string, object> itemData)
        {
            try
            {
                if (itemData.TryGetValue("LineNumber", out var lineNumObj) && lineNumObj is int lineNum)
                    item.LineNumber = lineNum;

                if (itemData.TryGetValue("IsEnable", out var isEnableObj) && isEnableObj is bool isEnable)
                    item.IsEnable = isEnable;

                if (itemData.TryGetValue("Comment", out var commentObj) && commentObj is string comment)
                    item.Comment = comment;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "��{�v���p�e�B�������ɃG���[");
            }
        }

        /// <summary>
        /// JSON�t�@�C���ɃR�}���h���X�g��ۑ�
        /// </summary>
        public async Task SaveToFileAsync(string filePath)
        {
            try
            {
                _logger.LogInformation("�t�@�C���ۑ��J�n: {FilePath}", filePath);

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(_items, options);
                await System.IO.File.WriteAllTextAsync(filePath, jsonContent);

                _logger.LogInformation("�t�@�C���ۑ�����: {Count}�̃A�C�e��", _items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�t�@�C���ۑ����ɃG���[: {FilePath}", filePath);
                throw;
            }
        }

        #endregion

        #region ���[�e�B���e�B

        /// <summary>
        /// �w��s�ԍ��̃A�C�e�����擾
        /// </summary>
        public ICommandListItem? GetItemByLineNumber(int lineNumber)
        {
            return _items.FirstOrDefault(x => x.LineNumber == lineNumber);
        }

        /// <summary>
        /// ���̍s�ԍ����擾
        /// </summary>
        public int GetNextLineNumber()
        {
            return _items.Count > 0 ? _items.Max(x => x.LineNumber) + 1 : 1;
        }

        /// <summary>
        /// �A�C�e������Ɉړ�
        /// </summary>
        public bool MoveUp(ICommandListItem item)
        {
            var index = _items.IndexOf(item);
            if (index > 0)
            {
                _items.RemoveAt(index);
                _items.Insert(index - 1, item);
                RefreshListState();
                return true;
            }
            return false;
        }

        /// <summary>
        /// �A�C�e�������Ɉړ�
        /// </summary>
        public bool MoveDown(ICommandListItem item)
        {
            var index = _items.IndexOf(item);
            if (index >= 0 && index < _items.Count - 1)
            {
                _items.RemoveAt(index);
                _items.Insert(index + 1, item);
                RefreshListState();
                return true;
            }
            return false;
        }

        #endregion
    }
}
