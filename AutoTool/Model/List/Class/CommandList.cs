using AutoTool.Model.CommandDefinition;
using AutoTool.Model.List.Class;
using CommunityToolkit.Mvvm.ComponentModel;
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
using System.Reflection; // ���t���N�V�����p

namespace AutoTool.List.Class
{
    public partial class CommandList : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ICommandListItem> _items = new();

        public ICommandListItem this[int index]
        {
            get => Items[index];
            set => Items[index] = value;
        }

        /// <summary>
        /// ���X�g�ύX��̋��ʏ���
        /// </summary>
        private void RefreshListState()
        {
            ReorderItems();
            CalculateNestLevel();
            PairIfItems();
            PairLoopItems();
        }

        public void Add(ICommandListItem item)
        {
            ExecuteOnUIThread(() =>
            {
                Items.Add(item);
                RefreshListState();
            });
        }

        public void Remove(ICommandListItem item)
        {
            ExecuteOnUIThread(() =>
            {
                Items.Remove(item);
                RefreshListState();
            });
        }

        /// <summary>
        /// �w��C���f�b�N�X�̃A�C�e�����폜
        /// </summary>
        public void RemoveAt(int index)
        {
            ExecuteOnUIThread(() =>
            {
                if (index >= 0 && index < Items.Count)
                {
                    Items.RemoveAt(index);
                    RefreshListState();
                }
            });
        }

        public void Insert(int index, ICommandListItem item)
        {
            ExecuteOnUIThread(() =>
            {
                Items.Insert(index, item);
                RefreshListState();
            });
        }

        public void Override(int index, ICommandListItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            ExecuteOnUIThread(() =>
            {
                if (index < 0 || index >= Items.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                Items[index] = item;
                RefreshListState();
            });
        }

        public void Clear()
        {
            ExecuteOnUIThread(() =>
            {
                Items.Clear();
            });
        }

        public void Move(int oldIndex, int newIndex)
        {
            ExecuteOnUIThread(() =>
            {
                if (oldIndex < 0 || oldIndex >= Items.Count || newIndex < 0 || newIndex >= Items.Count)
                    return;

                var item = Items[oldIndex];
                Items.RemoveAt(oldIndex);
                Items.Insert(newIndex, item);
                RefreshListState();
            });
        }

        public void Copy(int oldIndex, int newIndex)
        {
            ExecuteOnUIThread(() =>
            {
                if (oldIndex < 0 || oldIndex >= Items.Count || newIndex < 0 || newIndex >= Items.Count)
                    return;

                var item = Items[oldIndex];
                Items.Insert(newIndex, item);
                RefreshListState();
            });
        }

        public void ReorderItems()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                Items[i].LineNumber = i + 1;
            }
        }

        public void CalculateNestLevel()
        {
            var nestLevel = 0;

            foreach (var item in Items)
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
        }

        /// <summary>
        /// ���ʂ̃y�A�����O����
        /// </summary>
        private void PairItems<TStart, TEnd>(Func<ICommandListItem, bool> startPredicate, Func<ICommandListItem, bool> endPredicate)
            where TStart : class
            where TEnd : class
        {
            var startItems = Items.OfType<TStart>().Cast<ICommandListItem>()
                .Where(startPredicate)
                .OrderBy(x => x.LineNumber)
                .ToList();

            var endItems = Items.OfType<TEnd>().Cast<ICommandListItem>()
                .Where(endPredicate)
                .OrderBy(x => x.LineNumber)
                .ToList();

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
                        break;
                    }
                }
            }
        }

        public void PairIfItems()
        {
            PairItems<AutoTool.Model.List.Interface.IIfItem, AutoTool.Model.List.Interface.IIfEndItem>(
                x => CommandRegistry.IsIfCommand(x.ItemType),
                x => x.ItemType == CommandRegistry.CommandTypes.IfEnd
            );
        }

        public void PairLoopItems()
        {
            PairItems<AutoTool.Model.List.Interface.ILoopItem, AutoTool.Model.List.Interface.ILoopEndItem>(
                x => CommandRegistry.IsLoopCommand(x.ItemType),
                x => x.ItemType == CommandRegistry.CommandTypes.LoopEnd
            );
        }

        public IEnumerable<ICommandListItem> Clone()
        {
            var clone = new List<ICommandListItem>();

            foreach (var item in Items)
            {
                clone.Add(item.Clone());
            }

            return clone;
        }

        public void Save(string filePath)
        {
            try
            {
                var cloneItems = Clone().ToList();
                JsonSerializerHelper.SerializeToFile(cloneItems, filePath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"�t�@�C���ۑ��Ɏ��s���܂���: {ex.Message}", ex);
            }
        }

        public void Load(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"�t�@�C����������܂���: {filePath}");
                }

                System.Diagnostics.Debug.WriteLine($"[CommandList] �t�@�C���ǂݍ��݊J�n: {filePath}");

                // �t�@�C�����e���m�F
                var jsonContent = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    System.Diagnostics.Debug.WriteLine("[CommandList] ��̃t�@�C���ł�");
                    
                    // UI�X���b�h�Ŏ��s�i���S�m�F�t���j
                    ExecuteOnUIThread(() =>
                    {
                        Items.Clear();
                    });
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[CommandList] JSON���e�T�C�Y: {jsonContent.Length}����");
                System.Diagnostics.Debug.WriteLine($"[CommandList] JSON���e�̐擪200����: {jsonContent.Substring(0, Math.Min(200, jsonContent.Length))}");

                // JSON�̊�{�\���𕪐�
                try
                {
                    using var doc = JsonDocument.Parse(jsonContent);
                    var root = doc.RootElement;
                    System.Diagnostics.Debug.WriteLine($"[CommandList] JSON���[�g�v�f: ValueKind={root.ValueKind}");
                    
                    if (root.ValueKind == JsonValueKind.Object)
                    {
                        System.Diagnostics.Debug.WriteLine("[CommandList] JSON�̓I�u�W�F�N�g�`��");
                        if (root.TryGetProperty("$id", out var idProp))
                        {
                            System.Diagnostics.Debug.WriteLine($"[CommandList] $id �v���p�e�B: {idProp.GetString()}");
                        }
                        if (root.TryGetProperty("$values", out var valuesProp))
                        {
                            System.Diagnostics.Debug.WriteLine($"[CommandList] $values �v���p�e�B: ValueKind={valuesProp.ValueKind}");
                            if (valuesProp.ValueKind == JsonValueKind.Array)
                            {
                                System.Diagnostics.Debug.WriteLine($"[CommandList] $values �z��: {valuesProp.GetArrayLength()}");
                            }
                        }
                    }
                    else if (root.ValueKind == JsonValueKind.Array)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CommandList] JSON�͔z��`��: �v�f��={root.GetArrayLength()}");
                    }
                }
                catch (Exception parseEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[CommandList] JSON�\�����̓G���[: {parseEx.Message}");
                }

                List<ICommandListItem>? deserializedItems = null;

                try
                {
                    System.Diagnostics.Debug.WriteLine("[CommandList] === List<ICommandListItem>�Ƃ��ēǂݍ��ݎ��s ===");
                    // List<ICommandListItem>�Ƃ��Ď��s
                    deserializedItems = JsonSerializerHelper.DeserializeFromFile<List<ICommandListItem>>(filePath);
                    System.Diagnostics.Debug.WriteLine($"[CommandList] List<ICommandListItem>�Ƃ��ēǂݍ��ݐ���: {deserializedItems?.Count ?? 0}��");
                    
                    if (deserializedItems != null)
                    {
                        for (int i = 0; i < Math.Min(5, deserializedItems.Count); i++)
                        {
                            var item = deserializedItems[i];
                            System.Diagnostics.Debug.WriteLine($"[CommandList] �ǂݍ��݃A�C�e��[{i}]: Type={item?.GetType().Name}, ItemType={item?.ItemType}, Comment={item?.Comment}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CommandList] List<ICommandListItem>�ł̓ǂݍ��ݎ��s: {ex.GetType().Name} - {ex.Message}");
                    
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("[CommandList] === ObservableCollection<ICommandListItem>�Ƃ��ēǂݍ��ݎ��s ===");
                        // ObservableCollection<ICommandListItem>�Ƃ��Ď��s
                        var obsCollection = JsonSerializerHelper.DeserializeFromFile<ObservableCollection<ICommandListItem>>(filePath);
                        deserializedItems = obsCollection?.ToList();
                        System.Diagnostics.Debug.WriteLine($"[CommandList] ObservableCollection<ICommandListItem>�Ƃ��ēǂݍ��ݐ���: {deserializedItems?.Count ?? 0}��");
                    }
                    catch (Exception ex2)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CommandList] ObservableCollection<ICommandListItem>�ł̓ǂݍ��ݎ��s: {ex2.GetType().Name} - {ex2.Message}");
                        
                        // �Ō�̎�i�FJSON���蓮��͂���BasicCommandItem�Ƃ��ēǂݍ���
                        try
                        {
                            System.Diagnostics.Debug.WriteLine("[CommandList] === �蓮JSON��͂Ƃ��ēǂݍ��ݎ��s ===");
                            using var doc = JsonDocument.Parse(jsonContent);
                            
                            // �Q�ƕێ��`���̏ꍇ��$values���擾
                            JsonElement arrayElement;
                            if (doc.RootElement.ValueKind == JsonValueKind.Object && 
                                doc.RootElement.TryGetProperty("$values", out var valuesElement))
                            {
                                System.Diagnostics.Debug.WriteLine("[CommandList] �Q�ƕێ��`���Ƃ��Ď蓮���");
                                arrayElement = valuesElement;
                            }
                            else if (doc.RootElement.ValueKind == JsonValueKind.Array)
                            {
                                System.Diagnostics.Debug.WriteLine("[CommandList] �ʏ�z��`���Ƃ��Ď蓮���");
                                arrayElement = doc.RootElement;
                            }
                            else
                            {
                                throw new InvalidDataException($"���Ή���JSON�`��: {doc.RootElement.ValueKind}");
                            }

                            if (arrayElement.ValueKind == JsonValueKind.Array)
                            {
                                deserializedItems = new List<ICommandListItem>();
                                var arrayLength = arrayElement.GetArrayLength();
                                System.Diagnostics.Debug.WriteLine($"[CommandList] �蓮��͔z��v�f��: {arrayLength}");
                        
                                int elementIndex = 0;
                                foreach (var element in arrayElement.EnumerateArray())
                                {
                                    var basicItem = CreateBasicCommandItemFromJson(element);
                                    if (basicItem is not null)
                                    {
                                        deserializedItems.Add(basicItem);
                                        System.Diagnostics.Debug.WriteLine($"[CommandList] �蓮��͗v�f[{elementIndex}]: {basicItem.ItemType} - {basicItem.Comment}");
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[CommandList] �蓮��͗v�f[{elementIndex}]: �쐬���s");
                                    }
                                    elementIndex++;
                                }
                                System.Diagnostics.Debug.WriteLine($"[CommandList] �蓮��͂œǂݍ��ݐ���: {deserializedItems.Count}��");
                            }
                        }
                        catch (Exception ex3)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CommandList] �蓮��͂����s: {ex3.GetType().Name} - {ex3.Message}");
                            System.Diagnostics.Debug.WriteLine($"[CommandList] �X�^�b�N�g���[�X: {ex3.StackTrace}");
                            throw new InvalidDataException($"JSON�t�@�C���̉�͂Ɋ��S�Ɏ��s���܂���: {ex3.Message}", ex3);
                        }
                    }
                }

                if (deserializedItems != null && deserializedItems.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[CommandList] === UI�X���b�h�ŃA�C�e���ǉ��J�n: {deserializedItems.Count}�� ===");
                    
                    // UI�X���b�h��ObservableCollection�𑀍�i���S�m�F�t���j
                    ExecuteOnUIThread(() =>
                    {
                        Items.Clear();

                        foreach (var item in deserializedItems)
                        {
                            if (item != null)
                            {
                                // ��{�v���p�e�B�̌��؁E�C��
                                if (string.IsNullOrEmpty(item.ItemType))
                                {
                                    System.Diagnostics.Debug.WriteLine("[CommandList] ItemType����̃A�C�e�����C��");
                                    item.ItemType = item.GetType().Name.Replace("Item", "");
                                }

                                // Add���\�b�h����RefreshListState���Ă΂�邽�߁A����Items�ɒǉ�
                                Items.Add(item);
                                System.Diagnostics.Debug.WriteLine($"[CommandList] �A�C�e���ǉ�����: {item.ItemType} - {item.Comment}");
                            }
                        }

                        // ���X�g��Ԃ��X�V�iUI�X���b�h�Ŏ��s�j
                        RefreshListState();
                        
                        System.Diagnostics.Debug.WriteLine($"[CommandList] === �t�@�C���ǂݍ��݊���: {Items.Count}�̃A�C�e�� ===");
                    });
                }
                else
                {
                    // ��̃t�@�C���܂��͖�����JSON
                    System.Diagnostics.Debug.WriteLine("[CommandList] �L���ȃA�C�e����������܂���ł���");
                    ExecuteOnUIThread(() =>
                    {
                        Items.Clear();
                    });
                }
            }
            catch (JsonException jsonEx)
            {
                var errorMessage = $"JSON�t�@�C���̌`���������ł�: {jsonEx.Message}";
                System.Diagnostics.Debug.WriteLine($"[CommandList] {errorMessage}");
                System.Diagnostics.Debug.WriteLine($"[CommandList] JsonException�ڍ�: {jsonEx}");
                throw new InvalidDataException(errorMessage, jsonEx);
            }
            catch (Exception ex)
            {
                var errorMessage = $"�t�@�C���ǂݍ��݂Ɏ��s���܂���: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[CommandList] {errorMessage}");
                System.Diagnostics.Debug.WriteLine($"[CommandList] Exception�ڍ�: {ex}");
                throw new InvalidOperationException(errorMessage, ex);
            }
        }

        /// <summary>
        /// UI�X���b�h�ň��S�Ɏ��s����w���p�[���\�b�h
        /// </summary>
        private static void ExecuteOnUIThread(Action action)
        {
            try
            {
                var app = System.Windows.Application.Current;
                if (app != null)
                {
                    if (app.Dispatcher.CheckAccess())
                    {
                        // ����UI�X���b�h�Ŏ��s��
                        action();
                    }
                    else
                    {
                        // �ʃX���b�h����̌Ăяo�� - Dispatcher���g�p
                        app.Dispatcher.Invoke(action);
                    }
                }
                else
                {
                    // Application�����݂��Ȃ��ꍇ�i�e�X�g���Ȃǁj
                    System.Diagnostics.Debug.WriteLine("Application.Current��null�̂��߁A���ڎ��s���܂�");
                    action();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UI�X���b�h���s���ɃG���[: {ex.Message}");
                throw new InvalidOperationException($"UI�X���b�h�ł̎��s�Ɏ��s���܂���: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// JsonElement����K�؂Ȍ^��CommandListItem���쐬
        /// </summary>
        private static ICommandListItem? CreateBasicCommandItemFromJson(JsonElement element)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[CreateBasicCommandItemFromJson] �v�f�����J�n: ValueKind={element.ValueKind}");

                // �v�f�̑S�v���p�e�B��񋓂��ăf�o�b�O
                if (element.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in element.EnumerateObject())
                    {
                        System.Diagnostics.Debug.WriteLine($"[CreateBasicCommandItemFromJson] �v���p�e�B: {property.Name} = {property.Value} (Type: {property.Value.ValueKind})");
                    }
                }

                // ItemType �̎擾�i�����̃v���p�e�B�������s�j
                string? itemType = null;
                if (element.TryGetProperty("itemType", out var itemTypeElement))
                {
                    itemType = itemTypeElement.GetString();
                }
                else if (element.TryGetProperty("ItemType", out var itemTypeElement2))
                {
                    itemType = itemTypeElement2.GetString();
                }

                if (!string.IsNullOrEmpty(itemType))
                {
                    System.Diagnostics.Debug.WriteLine($"[CreateBasicCommandItemFromJson] ItemType�擾: {itemType}");
                }
                else
                {
                    // ���̃v���p�e�B����^�𐄒�
                    if (element.TryGetProperty("loopCount", out _) || element.TryGetProperty("LoopCount", out _))
                    {
                        itemType = "Loop";
                    }
                    else if (element.TryGetProperty("imagePath", out _) || element.TryGetProperty("ImagePath", out _))
                    {
                        if (element.TryGetProperty("modelPath", out _) || element.TryGetProperty("ModelPath", out _))
                        {
                            itemType = "Click_Image_AI";
                        }
                        else
                        {
                            itemType = "Click_Image";
                        }
                    }
                    else if (element.TryGetProperty("wait", out _) || element.TryGetProperty("Wait", out _))
                    {
                        itemType = "Wait";
                    }
                    else if (element.TryGetProperty("x", out _) && element.TryGetProperty("y", out _))
                    {
                        itemType = "Click";
                    }
                    else if (element.TryGetProperty("key", out _) || element.TryGetProperty("Key", out _))
                    {
                        itemType = "Hotkey";
                    }
                    else if (element.TryGetProperty("pair", out _) || element.TryGetProperty("Pair", out _))
                    {
                        // Pair�����邪LoopCount���Ȃ��ꍇ��IF�n��Loop_End�n
                        if (element.TryGetProperty("description", out var desc) && 
                            desc.ValueKind == JsonValueKind.String)
                        {
                            var descText = desc.GetString() ?? "";
                            if (descText.Contains("->") || descText.Contains("End"))
                            {
                                itemType = "IF_End"; // �������� Loop_End
                            }
                        }
                    }
                    else
                    {
                        itemType = "Unknown";
                    }
                    System.Diagnostics.Debug.WriteLine($"[CreateBasicCommandItemFromJson] ItemType����: {itemType}");
                }

                // CommandRegistry���g�p���ēK�؂Ȍ^�̃A�C�e�����쐬
                ICommandListItem? item = null;
                var itemTypes = CommandRegistry.GetTypeMapping();
                if (!string.IsNullOrEmpty(itemType) && itemTypes.TryGetValue(itemType, out var targetType))
                {
                    try
                    {
                        item = (ICommandListItem?)Activator.CreateInstance(targetType);
                        if (item != null)
                        {
                            item.ItemType = itemType;
                            System.Diagnostics.Debug.WriteLine($"[CreateBasicCommandItemFromJson] �K�؂Ȍ^�ō쐬����: {targetType.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CreateBasicCommandItemFromJson] �K�؂Ȍ^�ł̍쐬���s: {ex.Message}");
                    }
                }

                // �t�H�[���o�b�N: BasicCommandItem
                if (item == null)
                {
                    item = new BasicCommandItem();
                    item.ItemType = itemType ?? "Unknown";
                    System.Diagnostics.Debug.WriteLine($"[CreateBasicCommandItemFromJson] BasicCommandItem�t�H�[���o�b�N: {item.ItemType}");
                }

                // �v���p�e�B�𕜌�
                RestorePropertiesFromJson(item, element);

                System.Diagnostics.Debug.WriteLine($"[CreateBasicCommandItemFromJson] �A�C�e���쐬����: {item.GetType().Name} - ItemType={item.ItemType}, Comment={item.Comment}");
                return item;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CreateBasicCommandItemFromJson] �A�C�e���쐬���s: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CreateBasicCommandItemFromJson] �X�^�b�N�g���[�X: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// JsonElement����v���p�e�B�𕜌�
        /// </summary>
        private static void RestorePropertiesFromJson(ICommandListItem item, JsonElement element)
        {
            try
            {
                var itemType = item.GetType();

                // ��{�v���p�e�B�̕���
                if (element.TryGetProperty("comment", out var commentElement) || 
                    element.TryGetProperty("Comment", out commentElement))
                {
                    item.Comment = commentElement.GetString() ?? string.Empty;
                }

                if (element.TryGetProperty("description", out var descElement) || 
                    element.TryGetProperty("Description", out descElement))
                {
                    item.Description = descElement.GetString() ?? string.Empty;
                }

                if (element.TryGetProperty("isEnable", out var isEnableElement) || 
                    element.TryGetProperty("IsEnable", out isEnableElement))
                {
                    item.IsEnable = isEnableElement.GetBoolean();
                }

                if (element.TryGetProperty("lineNumber", out var lineNumberElement) || 
                    element.TryGetProperty("LineNumber", out lineNumberElement))
                {
                    item.LineNumber = lineNumberElement.GetInt32();
                }

                if (element.TryGetProperty("nestLevel", out var nestLevelElement) || 
                    element.TryGetProperty("NestLevel", out nestLevelElement))
                {
                    item.NestLevel = nestLevelElement.GetInt32();
                }

                if (element.TryGetProperty("isInLoop", out var isInLoopElement) || 
                    element.TryGetProperty("IsInLoop", out isInLoopElement))
                {
                    item.IsInLoop = isInLoopElement.GetBoolean();
                }

                if (element.TryGetProperty("isInIf", out var isInIfElement) || 
                    element.TryGetProperty("IsInIf", out isInIfElement))
                {
                    item.IsInIf = isInIfElement.GetBoolean();
                }

                // �^�ŗL�̃v���p�e�B����
                RestoreTypeSpecificProperties(item, element);

                System.Diagnostics.Debug.WriteLine($"[RestorePropertiesFromJson] �v���p�e�B��������: {item.ItemType}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RestorePropertiesFromJson] �v���p�e�B�����G���[: {ex.Message}");
            }
        }

        /// <summary>
        /// �^�ŗL�̃v���p�e�B�𕜌�
        /// </summary>
        private static void RestoreTypeSpecificProperties(ICommandListItem item, JsonElement element)
        {
            try
            {
                var itemType = item.GetType();

                // ���t���N�V�����Ŋe�v���p�e�B�𕜌�
                foreach (var property in element.EnumerateObject())
                {
                    var propInfo = itemType.GetProperty(property.Name, 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    
                    if (propInfo == null)
                    {
                        // PascalCase�ϊ������s
                        var pascalName = char.ToUpper(property.Name[0]) + property.Name.Substring(1);
                        propInfo = itemType.GetProperty(pascalName);
                    }

                    if (propInfo != null && propInfo.CanWrite)
                    {
                        try
                        {
                            object? value = null;
                            
                            // �^�ɉ������l�ϊ�
                            if (propInfo.PropertyType == typeof(string))
                            {
                                value = property.Value.GetString();
                            }
                            else if (propInfo.PropertyType == typeof(int) || propInfo.PropertyType == typeof(int?))
                            {
                                if (property.Value.ValueKind == JsonValueKind.Number)
                                {
                                    value = property.Value.GetInt32();
                                }
                                else if (property.Value.ValueKind == JsonValueKind.String && 
                                         int.TryParse(property.Value.GetString(), out var intVal))
                                {
                                    value = intVal;
                                }
                            }
                            else if (propInfo.PropertyType == typeof(double) || propInfo.PropertyType == typeof(double?))
                            {
                                if (property.Value.ValueKind == JsonValueKind.Number)
                                {
                                    value = property.Value.GetDouble();
                                }
                                else if (property.Value.ValueKind == JsonValueKind.String && 
                                         double.TryParse(property.Value.GetString(), out var doubleVal))
                                {
                                    value = doubleVal;
                                }
                            }
                            else if (propInfo.PropertyType == typeof(bool) || propInfo.PropertyType == typeof(bool?))
                            {
                                if (property.Value.ValueKind == JsonValueKind.True || property.Value.ValueKind == JsonValueKind.False)
                                {
                                    value = property.Value.GetBoolean();
                                }
                                else if (property.Value.ValueKind == JsonValueKind.String && 
                                         bool.TryParse(property.Value.GetString(), out var boolVal))
                                {
                                    value = boolVal;
                                }
                            }
                            else if (propInfo.PropertyType.IsEnum)
                            {
                                var stringValue = property.Value.GetString();
                                if (!string.IsNullOrEmpty(stringValue) && 
                                    Enum.TryParse(propInfo.PropertyType, stringValue, true, out var enumValue))
                                {
                                    value = enumValue;
                                }
                            }

                            if (value != null)
                            {
                                propInfo.SetValue(item, value);
                                System.Diagnostics.Debug.WriteLine($"[RestoreTypeSpecificProperties] �v���p�e�B�ݒ萬��: {property.Name} = {value}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[RestoreTypeSpecificProperties] �v���p�e�B�ݒ莸�s: {property.Name} - {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RestoreTypeSpecificProperties] �^�ŗL�v���p�e�B�����G���[: {ex.Message}");
            }
        }
    }
}
