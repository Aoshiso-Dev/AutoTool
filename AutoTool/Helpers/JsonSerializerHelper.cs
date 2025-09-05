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
using AutoTool.Model.CommandDefinition; // UniversalCommandItem�p
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
            LogDebug("Deserialize�J�n: �^={Type}", typeof(T).Name);
            try
            {
                var result = JsonSerializer.Deserialize<T>(json, OptionsWithoutReferences);
                LogDebug("Deserialize����");
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex, "Deserialize���s: �^={Type}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// ItemType����Ή�����Type���擾���鎫��
        /// </summary>
        private static readonly Dictionary<string, Type> ItemTypeToTypeMap = new()
        {
            { "Click", typeof(UniversalCommandItem) },
            { "Wait_Image", typeof(UniversalCommandItem) },
            { "Click_Image", typeof(UniversalCommandItem) },
            { "Click_Image_AI", typeof(UniversalCommandItem) },
            { "Hotkey", typeof(UniversalCommandItem) },
            { "Wait", typeof(UniversalCommandItem) },
            { "Loop", typeof(UniversalCommandItem) },
            { "Loop_End", typeof(UniversalCommandItem) },
            { "Loop_Break", typeof(UniversalCommandItem) },
            { "IF_ImageExist", typeof(UniversalCommandItem) },
            { "IF_ImageNotExist", typeof(UniversalCommandItem) },
            { "IF_End", typeof(UniversalCommandItem) },
            { "IF_ImageExist_AI", typeof(UniversalCommandItem) },
            { "IF_ImageNotExist_AI", typeof(UniversalCommandItem) },
            { "Execute", typeof(UniversalCommandItem) },
            { "SetVariable", typeof(UniversalCommandItem) },
            { "SetVariable_AI", typeof(UniversalCommandItem) },
            { "IF_Variable", typeof(UniversalCommandItem) },
            { "Screenshot", typeof(UniversalCommandItem) },
            // �t�H�[���o�b�N�p
            { "", typeof(UniversalCommandItem) }
        };

        /// <summary>
        /// ItemType������ۂ�Type���擾
        /// </summary>
        public static Type GetTypeFromItemType(string itemType)
        {
            return ItemTypeToTypeMap.TryGetValue(itemType, out var type) ? type : typeof(UniversalCommandItem);
        }
    }

    #region �J�X�^���R���o�[�^�[

    /// <summary>
    /// �}�E�X�{�^���p�̃J�X�^���R���o�[�^�[
    /// </summary>
    public class MouseButtonEnumConverter : JsonConverter<MouseButton>
    {
        public override MouseButton Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (Enum.TryParse<MouseButton>(stringValue, true, out var result))
                {
                    return result;
                }
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                var intValue = reader.GetInt32();
                if (Enum.IsDefined(typeof(MouseButton), intValue))
                {
                    return (MouseButton)intValue;
                }
            }

            return MouseButton.Left; // �f�t�H���g�l
        }

        public override void Write(Utf8JsonWriter writer, MouseButton value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    /// <summary>
    /// ICommandListItem�p�̃J�X�^���R���o�[�^�[
    /// </summary>
    public class CommandListItemConverter : JsonConverter<ICommandListItem>
    {
        public override ICommandListItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            // ItemType�v���p�e�B���擾
            var itemType = root.TryGetProperty("ItemType", out var itemTypeProp) 
                ? itemTypeProp.GetString() ?? ""
                : "";

            // �Ή�����Type���擾
            var targetType = JsonSerializerHelper.GetTypeFromItemType(itemType);

            // �Y������Type�Ńf�V���A���C�Y
            return (ICommandListItem?)JsonSerializer.Deserialize(root.GetRawText(), targetType, options);
        }

        public override void Write(Utf8JsonWriter writer, ICommandListItem value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }

    /// <summary>
    /// ICommandListItem��List�p�R���o�[�^�[
    /// </summary>
    public class CommandListItemListConverter : JsonConverter<List<ICommandListItem>>
    {
        public override List<ICommandListItem>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }

            var list = new List<ICommandListItem>();
            var itemConverter = new CommandListItemConverter();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return list;
                }

                var item = itemConverter.Read(ref reader, typeof(ICommandListItem), options);
                if (item != null)
                {
                    list.Add(item);
                }
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, List<ICommandListItem> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            var itemConverter = new CommandListItemConverter();
            foreach (var item in value)
            {
                itemConverter.Write(writer, item, options);
            }
            writer.WriteEndArray();
        }
    }

    /// <summary>
    /// ICommandListItem��ObservableCollection�p�R���o�[�^�[
    /// </summary>
    public class CommandListItemObservableCollectionConverter : JsonConverter<ObservableCollection<ICommandListItem>>
    {
        public override ObservableCollection<ICommandListItem>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var listConverter = new CommandListItemListConverter();
            var list = listConverter.Read(ref reader, typeof(List<ICommandListItem>), options);
            return list != null ? new ObservableCollection<ICommandListItem>(list) : null;
        }

        public override void Write(Utf8JsonWriter writer, ObservableCollection<ICommandListItem> value, JsonSerializerOptions options)
        {
            var listConverter = new CommandListItemListConverter();
            listConverter.Write(writer, value.ToList(), options);
        }
    }

    #endregion
}