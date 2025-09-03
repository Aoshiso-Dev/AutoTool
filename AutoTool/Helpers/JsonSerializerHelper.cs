using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoTool.Model.List.Interface;
using AutoTool.Model.List.Class;

namespace AutoTool.Helpers
{
    /// <summary>
    /// JSON �V���A���C�[�[�V�����w���p�[
    /// </summary>
    public static class JsonSerializerHelper
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new CommandListItemConverter(),
                new JsonStringEnumConverter()
            }
        };

        /// <summary>
        /// �I�u�W�F�N�g���t�@�C���ɃV���A���C�Y
        /// </summary>
        public static void SerializeToFile<T>(T obj, string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(obj, Options);
            File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// �t�@�C������f�V���A���C�Y
        /// </summary>
        public static T? DeserializeFromFile<T>(string filePath)
        {
            if (!File.Exists(filePath))
                return default;

            var json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
            return JsonSerializer.Deserialize<T>(json, Options);
        }

        /// <summary>
        /// �I�u�W�F�N�g��JSON������ɃV���A���C�Y
        /// </summary>
        public static string Serialize<T>(T obj)
        {
            return JsonSerializer.Serialize(obj, Options);
        }

        /// <summary>
        /// JSON�����񂩂�f�V���A���C�Y
        /// </summary>
        public static T? Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }
    }

    /// <summary>
    /// CommandListItem�p��JSON�R���o�[�^�[
    /// </summary>
    public class CommandListItemConverter : JsonConverter<ICommandListItem>
    {
        public override ICommandListItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            if (!root.TryGetProperty("itemType", out var itemTypeElement))
                return null;

            var itemType = itemTypeElement.GetString();
            if (string.IsNullOrEmpty(itemType))
                return null;

            // ItemType�Ɋ�Â��ēK�؂Ȍ^������
            var targetType = GetItemTypeFromString(itemType);
            if (targetType == null)
                return null;

            var jsonString = root.GetRawText();
            return (ICommandListItem?)JsonSerializer.Deserialize(jsonString, targetType, options);
        }

        public override void Write(Utf8JsonWriter writer, ICommandListItem value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }

        private static Type? GetItemTypeFromString(string itemType)
        {
            return itemType switch
            {
                "Wait_Image" => typeof(WaitImageItem),
                "Click_Image" => typeof(ClickImageItem),
                "Click_Image_AI" => typeof(ClickImageAIItem),
                "Hotkey" => typeof(HotkeyItem),
                "Click" => typeof(ClickItem),
                "Wait" => typeof(WaitItem),
                "Loop" => typeof(LoopItem),
                "Loop_End" => typeof(LoopEndItem),
                "Loop_Break" => typeof(LoopBreakItem),
                "IF_ImageExist" => typeof(IfImageExistItem),
                "IF_ImageNotExist" => typeof(IfImageNotExistItem),
                "IF_End" => typeof(IfEndItem),
                "IF_ImageExist_AI" => typeof(IfImageExistAIItem),
                "IF_ImageNotExist_AI" => typeof(IfImageNotExistAIItem),
                "Execute" => typeof(ExecuteItem),
                "SetVariable" => typeof(SetVariableItem),
                "SetVariable_AI" => typeof(SetVariableAIItem),
                "IF_Variable" => typeof(IfVariableItem),
                "Screenshot" => typeof(ScreenshotItem),
                _ => typeof(CommandListItem) // �t�H�[���o�b�N
            };
        }
    }
}