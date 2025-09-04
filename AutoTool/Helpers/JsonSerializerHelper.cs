using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using AutoTool.Model.List.Interface;
using AutoTool.Model.List.Type;
using AutoTool.Model.List.Class;
using System.Windows.Input;
using System.Linq; // �ǉ�

namespace AutoTool.Helpers
{
    /// <summary>
    /// JSON �V���A���C�U�[ �w���p�[�iDI�Ή��j
    /// </summary>
    public static class JsonSerializerHelper
    {
        // �ÓI���K�[�iDI����ݒ�j
        private static ILogger? _logger;

        /// <summary>
        /// DI���烍�K�[��ݒ�
        /// </summary>
        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = null,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            // �Q�ƕێ���L����
            ReferenceHandler = ReferenceHandler.Preserve,
            NumberHandling = JsonNumberHandling.AllowReadingFromString, // �����񂩂�̐��l�ǂݎ�������
            AllowTrailingCommas = true, // �����̃J���}������
            ReadCommentHandling = JsonCommentHandling.Skip, // �R�����g���X�L�b�v
            Converters =
            {
                new MouseButtonEnumConverter(), // �ǉ�: �}�E�X�{�^���p�J�X�^���R���o�[�^�[
                new CommandListItemConverter(),
                new CommandListItemListConverter(),
                new CommandListItemObservableCollectionConverter(),
                new JsonStringEnumConverter()
            }
        };

        // �Q�ƕێ��Ȃ��̃I�v�V�����i�]���̌`���p�j
        private static readonly JsonSerializerOptions OptionsWithoutReferences = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = null, // CamelCase�𖳌������Č��̖��O��ێ�
            PropertyNameCaseInsensitive = true, // �啶������������ʂ��Ȃ�
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString, // �����񂩂�̐��l�ǂݎ�������
            AllowTrailingCommas = true, // �����̃J���}������
            ReadCommentHandling = JsonCommentHandling.Skip, // �R�����g���X�L�b�v
            Converters =
            {
                new MouseButtonEnumConverter(), // �ǉ�: �}�E�X�{�^���p�J�X�^���R���o�[�^�[
                new CommandListItemConverter(),
                new CommandListItemListConverter(),
                new CommandListItemObservableCollectionConverter(),
                new JsonStringEnumConverter()
            }
        };

        // ===== ���O�w���p�istring.Format ���g�킸��O��h���j =====
        private static string CombineMessage(string message, object[] args)
        {
            if (args == null || args.Length == 0) return message;
            // �X�g���N�`���[�h���O�� {Name} �v���[�X�z���_�[�͂��̂܂܎c���A�����ɒl���
            var argList = string.Join(", ", args.Select((a, i) => $"arg{i}={a}"));
            return $"{message} | {argList}"; // ���S�ȘA��
        }

        private static void LogDebug(string message, params object[] args)
        {
            _logger?.LogDebug(message, args);
            System.Diagnostics.Debug.WriteLine("[JsonSerializerHelper] " + CombineMessage(message, args));
        }

        private static void LogInformation(string message, params object[] args)
        {
            _logger?.LogInformation(message, args);
            System.Diagnostics.Debug.WriteLine("[JsonSerializerHelper] " + CombineMessage(message, args));
        }

        private static void LogWarning(string message, params object[] args)
        {
            _logger?.LogWarning(message, args);
            System.Diagnostics.Debug.WriteLine("[JsonSerializerHelper] WARNING: " + CombineMessage(message, args));
        }

        private static void LogError(Exception? ex, string message, params object[] args)
        {
            _logger?.LogError(ex, message, args);
            System.Diagnostics.Debug.WriteLine("[JsonSerializerHelper] ERROR: " + CombineMessage(message, args) + (ex != null ? $" - {ex.Message}" : string.Empty));
        }

        /// <summary>
        /// �I�u�W�F�N�g���t�@�C���ɃV���A���C�Y
        /// </summary>
        public static void SerializeToFile<T>(T obj, string filePath)
        {
            LogDebug("SerializeToFile�J�n: �t�@�C���p�X={FilePath}, �^={Type}", filePath, typeof(T).Name);
            
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    LogDebug("�f�B���N�g���쐬: {Directory}", directory);
                    Directory.CreateDirectory(directory);
                }

                // �Q�ƕێ��Ȃ��ŃV���A���C�Y�i�݊����̂��߁j
                LogDebug("JSON�V���A���C�Y�J�n�i�Q�ƕێ��Ȃ��j");
                var json = JsonSerializer.Serialize(obj, OptionsWithoutReferences);
                
                LogDebug("JSON������: {Length}����", json.Length);
                LogDebug("JSON�擪100����: {JsonStart}", json.Substring(0, Math.Min(100, json.Length)));
                
                File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
                LogInformation("�t�@�C���ۑ�����: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                LogError(ex, "�t�@�C���ۑ����s: {FilePath}", filePath);
                throw new InvalidOperationException($"�t�@�C���ۑ��Ɏ��s���܂���: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// �t�@�C������f�V���A���C�Y�i�����̌`�������s�j
        /// </summary>
        public static T? DeserializeFromFile<T>(string filePath)
        {
            LogDebug("DeserializeFromFile�J�n: �t�@�C���p�X={FilePath}, �^={Type}", filePath, typeof(T).Name);
            
            if (!File.Exists(filePath))
            {
                LogWarning("�t�@�C����������܂���: {FilePath}", filePath);
                return default;
            }

            try
            {
                var json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                LogDebug("�t�@�C���ǂݍ��݊���: ������={Length}", json.Length);
                LogDebug("JSON�擪200����: {JsonStart}", json.Substring(0, Math.Min(200, json.Length)));

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                LogDebug("JSON�\������: ValueKind={ValueKind}", root.ValueKind);

                try
                {
                    if (root.ValueKind == JsonValueKind.Object && 
                        root.TryGetProperty("$id", out var idProp) && 
                        root.TryGetProperty("$values", out var valuesProp))
                    {
                        LogInformation("�Q�ƕێ��`����JSON�����o: $id={Id}, $values�v�f��={Count}", 
                            idProp.GetString(), 
                            valuesProp.ValueKind == JsonValueKind.Array ? valuesProp.GetArrayLength() : 0);
                        
                        var result = JsonSerializer.Deserialize<T>(json, Options);
                        LogInformation("�Q�ƕێ��`���ł̃f�V���A���C�Y����");
                        return result;
                    }
                    
                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        LogInformation("�ʏ�̔z��`����JSON�����o: �v�f��={Count}", root.GetArrayLength());
                        var result = JsonSerializer.Deserialize<T>(json, OptionsWithoutReferences);
                        LogInformation("�ʏ�̔z��`���ł̃f�V���A���C�Y����");
                        return result;
                    }

                    LogInformation("�ʏ�̃I�u�W�F�N�g�`����JSON�����o");
                    var objResult = JsonSerializer.Deserialize<T>(json, OptionsWithoutReferences);
                    LogInformation("�ʏ�̃I�u�W�F�N�g�`���ł̃f�V���A���C�Y����");
                    return objResult;
                }
                catch (JsonException ex)
                {
                    LogError(ex, "JSON�`����������f�V���A���C�Y���s");
                    try
                    {
                        LogDebug("�t�H�[���o�b�N: �Q�ƕێ��Ȃ��ōĎ��s");
                        var fallbackResult = JsonSerializer.Deserialize<T>(json, OptionsWithoutReferences);
                        LogInformation("�t�H�[���o�b�N�f�V���A���C�Y����");
                        return fallbackResult;
                    }
                    catch (JsonException ex2)
                    {
                        LogError(ex2, "�t�H�[���o�b�N�f�V���A���C�Y���s");
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "�t�@�C���ǂݍ��ݏ������ɃG���[: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// �I�u�W�F�N�g��JSON������ɃV���A���C�Y
        /// </summary>
        public static string Serialize<T>(T obj)
        {
            LogDebug("Serialize�J�n: �^={Type}", typeof(T).Name);
            try
            {
                var result = JsonSerializer.Serialize(obj, OptionsWithoutReferences);
                LogDebug("Serialize����: ������={Length}", result.Length);
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex, "Serialize���s: �^={Type}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// JSON�����񂩂�f�V���A���C�Y
        /// </summary>
        public static T? Deserialize<T>(string json)
        {
            LogDebug("Deserialize�J�n: �^={Type}, JSON��={Length}", typeof(T).Name, json.Length);
            try
            {
                var result = JsonSerializer.Deserialize<T>(json, OptionsWithoutReferences);
                LogDebug("Deserialize����: �^={Type}", typeof(T).Name);
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex, "Deserialize���s: �^={Type}", typeof(T).Name);
                throw;
            }
        }
    }

    /// <summary>
    /// CommandListItem�p�J�X�^��JSON �R���o�[�^�[
    /// </summary>
    public class CommandListItemConverter : JsonConverter<ICommandListItem>
    {
        public override ICommandListItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] Read�J�n: ValueKind={root.ValueKind}");

            // �v�f�̏ڍ׏������O�o��
            if (root.ValueKind == JsonValueKind.Object)
            {
                System.Diagnostics.Debug.WriteLine("[CommandListItemConverter] �S�v���p�e�B�ꗗ:");
                foreach (var property in root.EnumerateObject())
                {
                    var valuePreview = property.Value.ValueKind == JsonValueKind.String 
                        ? $"\"{property.Value.GetString()}\"" 
                        : property.Value.ToString();
                    System.Diagnostics.Debug.WriteLine($"  {property.Name}: {valuePreview} ({property.Value.ValueKind})");
                }
            }

            // ItemType�𕡐��̕��@�Ŏ擾�����s
            string? itemType = null;
            JsonElement itemTypeElement = default;
            
            // 1. camelCase
            if (root.TryGetProperty("itemType", out itemTypeElement))
            {
                itemType = itemTypeElement.GetString();
                System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] itemType (camelCase) �擾: {itemType}");
            }
            // 2. PascalCase
            else if (root.TryGetProperty("ItemType", out itemTypeElement))
            {
                itemType = itemTypeElement.GetString();
                System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] ItemType (PascalCase) �擾: {itemType}");
            }
            // 3. �S������
            else if (root.TryGetProperty("itemtype", out itemTypeElement))
            {
                itemType = itemTypeElement.GetString();
                System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] itemtype (lowercase) �擾: {itemType}");
            }

            if (string.IsNullOrEmpty(itemType))
            {
                System.Diagnostics.Debug.WriteLine("[CommandListItemConverter] itemType�v���p�e�B��������Ȃ��܂��͋�ł��B�^��������s");
                
                // �v���p�e�B����^�𐄒�
                if (root.TryGetProperty("loopCount", out _) || root.TryGetProperty("LoopCount", out _))
                {
                    itemType = "Loop";
                    System.Diagnostics.Debug.WriteLine("[CommandListItemConverter] LoopCount���� Loop �Ɛ���");
                }
                else if (root.TryGetProperty("imagePath", out _) || root.TryGetProperty("ImagePath", out _))
                {
                    itemType = "Click_Image";
                    System.Diagnostics.Debug.WriteLine("[CommandListItemConverter] ImagePath���� Click_Image �Ɛ���");
                }
                else if (root.TryGetProperty("wait", out _) || root.TryGetProperty("Wait", out _))
                {
                    itemType = "Wait";
                    System.Diagnostics.Debug.WriteLine("[CommandListItemConverter] Wait���� Wait �Ɛ���");
                }
                else if (root.TryGetProperty("x", out _) && root.TryGetProperty("y", out _))
                {
                    itemType = "Click";
                    System.Diagnostics.Debug.WriteLine("[CommandListItemConverter] X,Y���� Click �Ɛ���");
                }
                else if (root.TryGetProperty("key", out _) || root.TryGetProperty("Key", out _))
                {
                    itemType = "Hotkey";
                    System.Diagnostics.Debug.WriteLine("[CommandListItemConverter] Key���� Hotkey �Ɛ���");
                }
                else if (root.TryGetProperty("pair", out _) || root.TryGetProperty("Pair", out _))
                {
                    // Pair�����邪LoopCount���Ȃ��ꍇ��IF�n��Loop_End�n
                    if (root.TryGetProperty("description", out var desc) && 
                        desc.ValueKind == JsonValueKind.String)
                    {
                        var descText = desc.GetString() ?? "";
                        if (descText.Contains("->"))
                        {
                            itemType = "IF_End"; // �������� Loop_End
                            System.Diagnostics.Debug.WriteLine("[CommandListItemConverter] Pair��->���� IF_End �Ɛ���");
                        }
                    }
                }
                
                if (string.IsNullOrEmpty(itemType))
                {
                    itemType = "Unknown";
                    System.Diagnostics.Debug.WriteLine("[CommandListItemConverter] �^���莸�s�AUnknown �ɐݒ�");
                }
            }

            System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] �ŏI�I��itemType: {itemType}");

            // ItemType�Ɋ�Â��ēK�؂Ȍ^������
            var targetType = GetItemTypeFromString(itemType);
            if (targetType == null)
            {
                System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] ���m��itemType�ABasicCommandItem�Ƀt�H�[���o�b�N: {itemType}");
                targetType = typeof(BasicCommandItem);
            }

            System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] �Ώی^: {targetType.Name}");

            try
            {
                var jsonString = root.GetRawText();
                System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] JSON�v�f: {jsonString.Substring(0, Math.Min(300, jsonString.Length))}...");
                
                var result = (ICommandListItem?)JsonSerializer.Deserialize(jsonString, targetType, options);
                
                // ItemType���������ݒ肳��Ă��邱�Ƃ��m�F
                if (result != null)
                {
                    if (string.IsNullOrEmpty(result.ItemType))
                    {
                        System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] ItemType��ݒ�: {itemType}");
                        result.ItemType = itemType;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] �ϊ�����: {targetType.Name} (ItemType={result.ItemType})");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] CommandListItem�ϊ��G���[: {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] �X�^�b�N�g���[�X: {ex.StackTrace}");
                
                // �f�V���A���C�Y�Ɏ��s�����ꍇ�ABasicCommandItem�Ƃ��ăt�H�[���o�b�N
                var basicItem = new BasicCommandItem
                {
                    ItemType = itemType ?? "Unknown",
                    Comment = "�f�V���A���C�Y�G���[����̕���",
                    Description = $"���̌^: {itemType}"
                };
                
                // �\�ł���Ί�{�v���p�e�B�𕜌�
                try
                {
                    if (root.TryGetProperty("comment", out var commentElement) || 
                        root.TryGetProperty("Comment", out commentElement))
                        basicItem.Comment = commentElement.GetString() ?? basicItem.Comment;
                    
                    if (root.TryGetProperty("isEnable", out var isEnableElement) || 
                        root.TryGetProperty("IsEnable", out isEnableElement))
                        basicItem.IsEnable = isEnableElement.GetBoolean();
                    
                    if (root.TryGetProperty("lineNumber", out var lineNumberElement) || 
                        root.TryGetProperty("LineNumber", out lineNumberElement))
                        basicItem.LineNumber = lineNumberElement.GetInt32();

                    if (root.TryGetProperty("description", out var descElement) || 
                        root.TryGetProperty("Description", out descElement))
                        basicItem.Description = descElement.GetString() ?? "";

                    System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] BasicCommandItem�t�H�[���o�b�N����: {itemType}");
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] BasicCommandItem�v���p�e�B�����G���[: {ex2.Message}");
                }
                
                return basicItem;
            }
        }

        public override void Write(Utf8JsonWriter writer, ICommandListItem value, JsonSerializerOptions options)
        {
            System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] Write: {value.ItemType} ({value.GetType().Name})");
            
            // �z�Q�Ƒ΍�F�V���v���ȃv���p�e�B�̂݃V���A���C�Y����ꎞ�I�u�W�F�N�g���쐬
            try
            {
                // �z�Q�Ƃ̌����ƂȂ�₷��Pair�v���p�e�B�Ȃǂ����O�����V���A���C�Y
                var safeOptions = new JsonSerializerOptions
                {
                    WriteIndented = options.WriteIndented,
                    Encoder = options.Encoder,
                    PropertyNamingPolicy = options.PropertyNamingPolicy,
                    PropertyNameCaseInsensitive = options.PropertyNameCaseInsensitive,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    MaxDepth = 32, // �[�x������ǉ�
                    // ReferenceHandler�͎g�킸�A�z�Q�Ƃ������
                    NumberHandling = options.NumberHandling,
                    AllowTrailingCommas = options.AllowTrailingCommas,
                    ReadCommentHandling = options.ReadCommentHandling,
                    Converters = 
                    {
                        new MouseButtonEnumConverter(),
                        new JsonStringEnumConverter()
                        // CommandListItemConverter�͏��O���ďz�������
                    }
                };

                JsonSerializer.Serialize(writer, value, value.GetType(), safeOptions);
            }
            catch (JsonException ex) when (ex.Message.Contains("cycle"))
            {
                System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] �z�Q�ƌ��o�A�t�H�[���o�b�N���s: {ex.Message}");
                
                // �t�H�[���o�b�N�F��{�v���p�e�B�݂̂̎������쐬���ăV���A���C�Y
                var safeData = new Dictionary<string, object?>
                {
                    ["ItemType"] = value.ItemType,
                    ["IsEnable"] = value.IsEnable,
                    ["LineNumber"] = value.LineNumber,
                    ["Comment"] = value.Comment,
                    ["Description"] = value.Description,
                    ["IsRunning"] = value.IsRunning,
                    ["IsSelected"] = value.IsSelected,
                    ["NestLevel"] = value.NestLevel,
                    ["IsInLoop"] = value.IsInLoop,
                    ["IsInIf"] = value.IsInIf,
                    ["Progress"] = value.Progress
                };

                // �^�ŗL�v���p�e�B�����S�ɒǉ�
                var properties = value.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    if (prop.Name == "Pair") continue; // �z�Q�Ƃ̌����ƂȂ�Pair�͏��O
                    if (safeData.ContainsKey(prop.Name)) continue; // ���ɒǉ��ς�
                    
                    try
                    {
                        var propValue = prop.GetValue(value);
                        // ���G�ȃI�u�W�F�N�g�͕�����
                        if (propValue != null && !IsSimpleType(prop.PropertyType))
                        {
                            safeData[prop.Name] = propValue.ToString();
                        }
                        else
                        {
                            safeData[prop.Name] = propValue;
                        }
                    }
                    catch
                    {
                        // �v���p�e�B�擾�Ɏ��s�����ꍇ�̓X�L�b�v
                    }
                }

                JsonSerializer.Serialize(writer, safeData);
            }
        }

        private static bool IsSimpleType(Type type)
        {
            return type.IsPrimitive || 
                   type == typeof(string) || 
                   type == typeof(DateTime) || 
                   type == typeof(decimal) || 
                   type == typeof(Guid) ||
                   type.IsEnum ||
                   (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && IsSimpleType(type.GetGenericArguments()[0]));
        }

        private static Type? GetItemTypeFromString(string itemType)
        {
            var result = itemType switch
            {
                // AutoTool.Model.List.Class �̋�̓I�ȃN���X���g�p
                "Wait_Image" => typeof(AutoTool.Model.List.Class.WaitImageItem),
                "Click_Image" => typeof(AutoTool.Model.List.Class.ClickImageItem),
                "Click_Image_AI" => typeof(AutoTool.Model.List.Class.ClickImageAIItem),
                "Hotkey" => typeof(AutoTool.Model.List.Class.HotkeyItem),
                "Click" => typeof(AutoTool.Model.List.Class.ClickItem),
                "Wait" => typeof(AutoTool.Model.List.Class.WaitItem),
                "Loop" => typeof(AutoTool.Model.List.Class.LoopItem),
                "Loop_End" => typeof(AutoTool.Model.List.Class.LoopEndItem),
                "Loop_Break" => typeof(AutoTool.Model.List.Class.LoopBreakItem),
                "IF_ImageExist" => typeof(AutoTool.Model.List.Class.IfImageExistItem),
                "IF_ImageNotExist" => typeof(AutoTool.Model.List.Class.IfImageNotExistItem),
                "IF_End" => typeof(AutoTool.Model.List.Class.IfEndItem),
                "IF_ImageExist_AI" => typeof(AutoTool.Model.List.Class.IfImageExistAIItem),
                "IF_ImageNotExist_AI" => typeof(AutoTool.Model.List.Class.IfImageNotExistAIItem),
                "Execute" => typeof(AutoTool.Model.List.Class.ExecuteItem),
                "SetVariable" => typeof(AutoTool.Model.List.Class.SetVariableItem),
                "SetVariable_AI" => typeof(AutoTool.Model.List.Class.SetVariableAIItem),
                "IF_Variable" => typeof(AutoTool.Model.List.Class.IfVariableItem),
                "Screenshot" => typeof(AutoTool.Model.List.Class.ScreenshotItem),
                
                // �e�X�g�p
                "Test" => typeof(BasicCommandItem),
                "Unknown" => typeof(BasicCommandItem),
                
                // �t�H�[���o�b�N
                _ => typeof(BasicCommandItem)
            };

            System.Diagnostics.Debug.WriteLine($"[CommandListItemConverter] �^�}�b�s���O: {itemType} -> {result?.Name ?? "null"}");
            return result;
        }
    }

    /// <summary>
    /// List<ICommandListItem>�p�R���o�[�^�[
    /// </summary>
    public class CommandListItemListConverter : JsonConverter<List<ICommandListItem>>
    {
        public override List<ICommandListItem>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            System.Diagnostics.Debug.WriteLine($"[CommandListItemListConverter] Read�J�n: ValueKind={root.ValueKind}");

            // �Q�ƕێ��`���̏ꍇ
            if (root.ValueKind == JsonValueKind.Object && 
                root.TryGetProperty("$values", out var valuesElement))
            {
                System.Diagnostics.Debug.WriteLine("[CommandListItemListConverter] �Q�ƕێ��`����List<ICommandListItem>������");
                return ProcessArrayElement(valuesElement, options);
            }

            // �ʏ�̔z��`���̏ꍇ
            if (root.ValueKind == JsonValueKind.Array)
            {
                System.Diagnostics.Debug.WriteLine("[CommandListItemListConverter] �ʏ�̔z��`����List<ICommandListItem>������");
                return ProcessArrayElement(root, options);
            }

            System.Diagnostics.Debug.WriteLine($"[CommandListItemListConverter] ���Ή��̌`��: {root.ValueKind}");
            return null;
        }

        private static List<ICommandListItem>? ProcessArrayElement(JsonElement arrayElement, JsonSerializerOptions options)
        {
            if (arrayElement.ValueKind != JsonValueKind.Array)
            {
                System.Diagnostics.Debug.WriteLine($"[CommandListItemListConverter] �z��łȂ��v�f: {arrayElement.ValueKind}");
                return null;
            }

            var list = new List<ICommandListItem>();
            var converter = new CommandListItemConverter();
            var arrayLength = arrayElement.GetArrayLength();

            System.Diagnostics.Debug.WriteLine($"[CommandListItemListConverter] �z�񏈗��J�n: �v�f��={arrayLength}");

            int processed = 0;
            int success = 0;
            int errors = 0;

            foreach (var element in arrayElement.EnumerateArray())
            {
                processed++;
                try
                {
                    var elementJson = element.GetRawText();
                    using var elementDoc = JsonDocument.Parse(elementJson);
                    var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(elementJson));
                    reader.Read();

                    var item = converter.Read(ref reader, typeof(ICommandListItem), options);
                    if (item != null)
                    {
                        list.Add(item);
                        success++;
                        System.Diagnostics.Debug.WriteLine($"[CommandListItemListConverter] �v�f�������� [{processed}/{arrayLength}]: {item.ItemType}");
                    }
                    else
                    {
                        errors++;
                        System.Diagnostics.Debug.WriteLine($"[CommandListItemListConverter] �v�f�������ʂ�null [{processed}/{arrayLength}]");
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    System.Diagnostics.Debug.WriteLine($"[CommandListItemListConverter] �z��v�f�̏����ŃG���[ [{processed}/{arrayLength}]: {ex.Message}");
                    // �G���[�����������v�f�̓X�L�b�v���đ��s
                }
            }

            System.Diagnostics.Debug.WriteLine($"[CommandListItemListConverter] �z�񏈗�����: ����={processed}, ����={success}, �G���[={errors}");
            return list;
        }

        public override void Write(Utf8JsonWriter writer, List<ICommandListItem> value, JsonSerializerOptions options)
        {
            System.Diagnostics.Debug.WriteLine($"[CommandListItemListConverter] Write�J�n: �v�f��={value.Count}");
            writer.WriteStartArray();
            var converter = new CommandListItemConverter();
            
            foreach (var item in value)
            {
                converter.Write(writer, item, options);
            }
            
            writer.WriteEndArray();
            System.Diagnostics.Debug.WriteLine("[CommandListItemListConverter] Write����");
        }
    }

    /// <summary>
    /// ObservableCollection<ICommandListItem>�p�R���o�[�^�[
    /// </summary>
    public class CommandListItemObservableCollectionConverter : JsonConverter<ObservableCollection<ICommandListItem>>
    {
        public override ObservableCollection<ICommandListItem>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            System.Diagnostics.Debug.WriteLine("[CommandListItemObservableCollectionConverter] Read�J�n");
            var listConverter = new CommandListItemListConverter();
            var list = listConverter.Read(ref reader, typeof(List<ICommandListItem>), options);
            
            if (list != null)
            {
                var result = new ObservableCollection<ICommandListItem>(list);
                System.Diagnostics.Debug.WriteLine($"[CommandListItemObservableCollectionConverter] ObservableCollection�쐬����: �v�f��={result.Count}");
                return result;
            }
            
            System.Diagnostics.Debug.WriteLine("[CommandListItemObservableCollectionConverter] Read���s");
            return null;
        }

        public override void Write(Utf8JsonWriter writer, ObservableCollection<ICommandListItem> value, JsonSerializerOptions options)
        {
            System.Diagnostics.Debug.WriteLine($"[CommandListItemObservableCollectionConverter] Write�J�n: �v�f��={value.Count}");
            var listConverter = new CommandListItemListConverter();
            listConverter.Write(writer, value.ToList(), options);
            System.Diagnostics.Debug.WriteLine("[CommandListItemObservableCollectionConverter] Write����");
        }
    }

    /// <summary>
    /// MouseButton �𐔒l/�����񗼑Ή��ŕ������A�ۑ����͕�����ŏo�͂���R���o�[�^�[
    /// </summary>
    public class MouseButtonEnumConverter : JsonConverter<MouseButton>
    {
        public override MouseButton Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        var s = reader.GetString();
                        if (string.IsNullOrWhiteSpace(s)) return MouseButton.Left;
                        if (Enum.TryParse<MouseButton>(s, true, out var mbFromString)) return mbFromString;
                        if (int.TryParse(s, out var intFromString) && Enum.IsDefined(typeof(MouseButton), intFromString))
                            return (MouseButton)intFromString;
                        break;
                    case JsonTokenType.Number:
                        if (reader.TryGetInt32(out var num) && Enum.IsDefined(typeof(MouseButton), num))
                            return (MouseButton)num;
                        break;
                }
            }
            catch
            {
                // �������Ċ����
            }
            return MouseButton.Left; // �t�H�[���o�b�N
        }

        public override void Write(Utf8JsonWriter writer, MouseButton value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}