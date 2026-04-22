using Microsoft.ML.OnnxRuntime;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AutoTool.Infrastructure.Vision.Yolo;

/// <summary>
/// ONNX メタデータとラベルファイルからクラスラベルを取得します。
/// </summary>
internal static partial class YoloLabelCatalog
{
    public static IReadOnlyDictionary<int, string> Load(string modelPath, string? labelsPath)
    {
        var metadataLabels = LoadFromOnnxMetadata(modelPath);
        if (metadataLabels.Count > 0)
        {
            return metadataLabels;
        }

        var explicitFileLabels = LoadFromOptionalFile(labelsPath);
        if (explicitFileLabels.Count > 0)
        {
            return explicitFileLabels;
        }

        var sidecarLabels = LoadFromSidecarFile(modelPath);
        if (sidecarLabels.Count > 0)
        {
            return sidecarLabels;
        }

        return EmptyLabels();
    }

    public static bool TryResolveClassId(
        string modelPath,
        string labelName,
        string? labelsPath,
        out int classId)
    {
        classId = -1;
        if (string.IsNullOrWhiteSpace(labelName))
        {
            return false;
        }

        var normalized = NormalizeLabel(labelName);
        if (int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericClassId))
        {
            classId = numericClassId;
            return true;
        }

        if (TryParsePrefixedLabel(normalized, out numericClassId))
        {
            classId = numericClassId;
            return true;
        }

        var labels = Load(modelPath, labelsPath);
        foreach (var pair in labels)
        {
            if (string.Equals(pair.Value, normalized, StringComparison.OrdinalIgnoreCase))
            {
                classId = pair.Key;
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyDictionary<int, string> LoadFromOnnxMetadata(string modelPath)
    {
        try
        {
            using var session = new InferenceSession(modelPath);
            if (!session.ModelMetadata.CustomMetadataMap.TryGetValue("names", out var namesRaw)
                || string.IsNullOrWhiteSpace(namesRaw))
            {
                return EmptyLabels();
            }

            var parsed = ParseNamesMetadata(namesRaw);
            return parsed.Count == 0 ? EmptyLabels() : parsed;
        }
        catch
        {
            return EmptyLabels();
        }
    }

    private static IReadOnlyDictionary<int, string> LoadFromOptionalFile(string? labelsPath)
    {
        if (string.IsNullOrWhiteSpace(labelsPath) || !File.Exists(labelsPath))
        {
            return EmptyLabels();
        }

        return ParseLabelFile(labelsPath);
    }

    private static IReadOnlyDictionary<int, string> LoadFromSidecarFile(string modelPath)
    {
        var directory = Path.GetDirectoryName(modelPath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return EmptyLabels();
        }

        var modelName = Path.GetFileNameWithoutExtension(modelPath);
        var candidates = new[]
        {
            Path.Combine(directory, $"{modelName}.labels.txt"),
            Path.Combine(directory, $"{modelName}.names"),
            Path.Combine(directory, "labels.txt"),
            Path.Combine(directory, "coco.names"),
            Path.Combine(directory, "data.yaml"),
            Path.Combine(directory, "dataset.yaml")
        };

        foreach (var candidate in candidates)
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            var parsed = ParseLabelFile(candidate);
            if (parsed.Count > 0)
            {
                return parsed;
            }
        }

        return EmptyLabels();
    }

    private static IReadOnlyDictionary<int, string> ParseNamesMetadata(string raw)
    {
        var trimmed = raw.Trim();

        // JSON 形式: {"0":"person","1":"bicycle"}
        if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
        {
            try
            {
                var json = trimmed.Replace('\'', '"');
                var fromJson = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (fromJson is not null)
                {
                    return fromJson
                        .Select(pair => (Index: ParseNonNegativeInt(pair.Key), Label: NormalizeLabel(pair.Value)))
                        .Where(pair => pair.Index >= 0 && !string.IsNullOrWhiteSpace(pair.Label))
                        .ToDictionary(pair => pair.Index, pair => pair.Label);
                }
            }
            catch
            {
                // Python dict 風の形式へフォールバック
            }

            var map = new Dictionary<int, string>();
            foreach (Match match in DictionaryPairRegex().Matches(trimmed))
            {
                var key = ParseNonNegativeInt(match.Groups["id"].Value);
                var value = NormalizeLabel(match.Groups["label"].Value);
                if (key >= 0 && !string.IsNullOrWhiteSpace(value))
                {
                    map[key] = value;
                }
            }

            return map;
        }

        return EmptyLabels();
    }

    private static IReadOnlyDictionary<int, string> ParseLabelFile(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".yaml", StringComparison.OrdinalIgnoreCase) || extension.Equals(".yml", StringComparison.OrdinalIgnoreCase)
            ? ParseYamlLabels(path)
            : ParseLineLabels(path);
    }

    private static IReadOnlyDictionary<int, string> ParseYamlLabels(string path)
    {
        var lines = File.ReadAllLines(path);
        var labels = new Dictionary<int, string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("names:", StringComparison.OrdinalIgnoreCase))
            {
                var value = trimmed["names:".Length..].Trim();
                if (value.StartsWith('[') && value.EndsWith(']'))
                {
                    var content = value[1..^1];
                    var parts = content.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    for (var i = 0; i < parts.Length; i++)
                    {
                        labels[i] = NormalizeLabel(parts[i].Trim('"', '\''));
                    }

                    return labels;
                }

                if (value.StartsWith('{') && value.EndsWith('}'))
                {
                    foreach (Match match in DictionaryPairRegex().Matches(value))
                    {
                        var key = ParseNonNegativeInt(match.Groups["id"].Value);
                        var label = NormalizeLabel(match.Groups["label"].Value);
                        if (key >= 0 && !string.IsNullOrWhiteSpace(label))
                        {
                            labels[key] = label;
                        }
                    }

                    return labels;
                }
            }
        }

        return EmptyLabels();
    }

    private static IReadOnlyDictionary<int, string> ParseLineLabels(string path)
    {
        var lines = File.ReadAllLines(path);
        var labels = new Dictionary<int, string>();
        var sequential = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = NormalizeLabel(line);
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            var match = IndexedLineRegex().Match(trimmed);
            if (match.Success)
            {
                var index = ParseNonNegativeInt(match.Groups["id"].Value);
                var label = NormalizeLabel(match.Groups["label"].Value);
                if (index >= 0 && !string.IsNullOrWhiteSpace(label))
                {
                    labels[index] = label;
                }

                continue;
            }

            sequential.Add(trimmed);
        }

        if (labels.Count > 0)
        {
            return labels;
        }

        for (var i = 0; i < sequential.Count; i++)
        {
            labels[i] = sequential[i];
        }

        return labels;
    }

    private static int ParseNonNegativeInt(string value)
    {
        return int.TryParse(value.Trim(' ', '"', '\''), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed >= 0
            ? parsed
            : -1;
    }

    private static string NormalizeLabel(string value) => value.Trim().Trim('"', '\'');

    private static bool TryParsePrefixedLabel(string value, out int classId)
    {
        classId = -1;
        var match = PrefixedLabelRegex().Match(value);
        if (!match.Success)
        {
            return false;
        }

        return int.TryParse(match.Groups["id"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out classId);
    }

    private static IReadOnlyDictionary<int, string> EmptyLabels() => new Dictionary<int, string>();

    [GeneratedRegex(@"['""]?(?<id>\d+)['""]?\s*:\s*['""]?(?<label>[^,'""]+)['""]?", RegexOptions.Compiled)]
    private static partial Regex DictionaryPairRegex();

    [GeneratedRegex(@"^(?<id>\d+)\s*[:\t, ]\s*(?<label>.+)$", RegexOptions.Compiled)]
    private static partial Regex IndexedLineRegex();

    [GeneratedRegex(@"^(?<id>\d+)\s*:\s*(?<label>.+)$", RegexOptions.Compiled)]
    private static partial Regex PrefixedLabelRegex();
}
