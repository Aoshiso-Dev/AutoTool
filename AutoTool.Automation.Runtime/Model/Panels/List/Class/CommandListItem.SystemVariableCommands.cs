using AutoTool.Commands.Model.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Commands;
using AutoTool.Commands.Services;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Automation.Runtime.Attributes;
using System.Globalization;
using System.Text;
using System.Text.Json;
using CommandDef = AutoTool.Automation.Runtime.Definitions;

namespace AutoTool.Automation.Runtime.Lists;

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.Execute, typeof(SimpleCommand), typeof(IExecuteCommandSettings), CommandDef.CommandCategory.System, displayPriority: 7, displaySubPriority: 1, displayNameJa: "プログラム実行", displayNameEn: "Execute Program")]
    /// <summary>
    /// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
    /// </summary>
    public partial class ExecuteItem : CommandListItem, IExecuteItem, IExecuteCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("プログラムパス", EditorType.FilePicker, Group = "実行設定", Order = 1,
                         Description = "実行するプログラム")]
        private string _programPath = string.Empty;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("引数", EditorType.TextBox, Group = "実行設定", Order = 2,
                         Description = "コマンドライン引数")]
        private string _arguments = string.Empty;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("作業ディレクトリ", EditorType.DirectoryPicker, Group = "実行設定", Order = 3,
                         Description = "作業ディレクトリ")]
        private string _workingDirectory = string.Empty;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("終了を待つ", EditorType.CheckBox, Group = "実行設定", Order = 4,
                         Description = "プログラム終了まで待機")]
        private bool _waitForExit = false;

        new public string Description => $"ファイルパス:{System.IO.Path.GetFileName(ProgramPath)} / 引数:{Arguments} / 作業フォルダ:{WorkingDirectory}";
        public ExecuteItem() { }
        public ExecuteItem(ExecuteItem? item = null) : base(item)
        {
            if (item is not null)
            {
                ProgramPath = item.ProgramPath;
                Arguments = item.Arguments;
                WorkingDirectory = item.WorkingDirectory;
                WaitForExit = item.WaitForExit;
            }
        }
        public new ICommandListItem Clone()
        {
            return new ExecuteItem(this);
        }
        
        public override async ValueTask<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            try
            {
                var absoluteProgramPath = context.ToAbsolutePath(ProgramPath);
                var absoluteWorkingDirectory = string.IsNullOrWhiteSpace(WorkingDirectory)
                    ? WorkingDirectory
                    : context.ToAbsolutePath(WorkingDirectory);

                await context.ExecuteProgramAsync(absoluteProgramPath, Arguments, absoluteWorkingDirectory, WaitForExit, cancellationToken).ConfigureAwait(false);
                context.Log($"プログラムを実行しました: {absoluteProgramPath}");
                return true;
            }
            catch (Exception ex)
            {
                context.Log($"プログラム実行エラー: {ex.Message}");
                return false;
            }
        }
    }

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.SetVariable, typeof(SimpleCommand), typeof(ISetVariableCommandSettings), CommandDef.CommandCategory.Variable, displayPriority: 6, displaySubPriority: 1, displayNameJa: "変数設定", displayNameEn: "Set Variable")]
    /// <summary>
    /// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
    /// </summary>
    public partial class SetVariableItem : CommandListItem, ISetVariableItem, ISetVariableCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("変数名", EditorType.TextBox, Group = "変数設定", Order = 1,
                         Description = "設定する変数の名前")]
        private string _name = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("値", EditorType.TextBox, Group = "変数設定", Order = 2,
                         Description = "設定する値")]
        private string _value = string.Empty;

        new public string Description => $"変数:{Name} = \"{Value}\"";

        public SetVariableItem() { }
        public SetVariableItem(SetVariableItem? item = null) : base(item)
        {
            if (item is not null)
            {
                Name = item.Name;
                Value = item.Value;
            }
        }

        public new ICommandListItem Clone() => new SetVariableItem(this);
        
        public override ValueTask<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            context.SetVariable(Name, Value);
            context.Log($"変数 {Name} = \"{Value}\" を設定しました");
            return ValueTask.FromResult(true);
        }
    }

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.ExtractJsonValue, typeof(SimpleCommand), typeof(IExtractJsonValueCommandSettings), CommandDef.CommandCategory.Variable, displayPriority: 6, displaySubPriority: 2, displayNameJa: "JSON値抽出", displayNameEn: "Extract JSON Value")]
    /// <summary>
    /// JSON 文字列を保持する変数から指定パスの値を抽出し、別の変数へ格納します。
    /// </summary>
    public partial class ExtractJsonValueItem : CommandListItem, IExtractJsonValueItem, IExtractJsonValueCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("JSON変数名", EditorType.TextBox, Group = "JSON抽出", Order = 1,
                         Description = "JSON文字列を保持している変数名")]
        private string _jsonVariableName = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("抽出パス", EditorType.TextBox, Group = "JSON抽出", Order = 2,
                         Description = "例: [last].DetectionValues[Name=EdgeX].Value")]
        private string _extractionPath = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("出力先変数名", EditorType.TextBox, Group = "JSON抽出", Order = 3,
                         Description = "抽出した値を格納する変数名")]
        private string _outputVariableName = string.Empty;

        new public string Description => $"JSON:{JsonVariableName} / パス:{ExtractionPath} -> {OutputVariableName}";

        public ExtractJsonValueItem() { }

        public ExtractJsonValueItem(ExtractJsonValueItem? item = null) : base(item)
        {
            if (item is not null)
            {
                JsonVariableName = item.JsonVariableName;
                ExtractionPath = item.ExtractionPath;
                OutputVariableName = item.OutputVariableName;
            }
        }

        public new ICommandListItem Clone() => new ExtractJsonValueItem(this);

        public override ValueTask<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var json = context.GetVariable(JsonVariableName);
                if (string.IsNullOrWhiteSpace(json))
                {
                    context.Log($"JSON抽出エラー: 変数 {JsonVariableName} が空です。");
                    return ValueTask.FromResult(false);
                }

                var value = JsonValueExtractor.Extract(json, ExtractionPath);
                context.SetVariable(OutputVariableName, value);
                context.Log($"JSON抽出結果: {OutputVariableName} = {value}");
                return ValueTask.FromResult(true);
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
            {
                context.Log($"JSON抽出エラー: {ex.Message}");
                return ValueTask.FromResult(false);
            }
        }
    }

internal static class JsonValueExtractor
{
    public static string Extract(string json, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var document = JsonDocument.Parse(json);
        var current = document.RootElement;
        foreach (var segment in JsonPathParser.Parse(path))
        {
            current = segment.Apply(current);
        }

        return current.ValueKind switch
        {
            JsonValueKind.String => current.GetString() ?? string.Empty,
            JsonValueKind.Number => current.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            JsonValueKind.Null => string.Empty,
            _ => current.GetRawText()
        };
    }
}

internal abstract record JsonPathSegment
{
    public abstract JsonElement Apply(JsonElement current);
}

internal sealed record JsonPropertySegment(string PropertyName) : JsonPathSegment
{
    public override JsonElement Apply(JsonElement current)
    {
        if (current.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException($"JSON要素はオブジェクトではありません: {PropertyName}");
        }

        if (current.TryGetProperty(PropertyName, out var value))
        {
            return value;
        }

        foreach (var property in current.EnumerateObject())
        {
            if (string.Equals(property.Name, PropertyName, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        throw new InvalidOperationException($"JSONプロパティが見つかりません: {PropertyName}");
    }
}

internal sealed record JsonArrayIndexSegment(string IndexText) : JsonPathSegment
{
    public override JsonElement Apply(JsonElement current)
    {
        if (current.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"JSON要素は配列ではありません: [{IndexText}]");
        }

        var values = current.EnumerateArray().ToArray();
        if (values.Length == 0)
        {
            throw new InvalidOperationException("JSON配列が空です。");
        }

        var index = IndexText.ToLowerInvariant() switch
        {
            "first" => 0,
            "last" => values.Length - 1,
            _ when int.TryParse(IndexText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => throw new FormatException($"配列インデックスを解釈できません: {IndexText}")
        };

        if (index < 0 || index >= values.Length)
        {
            throw new InvalidOperationException($"配列インデックスが範囲外です: {index}");
        }

        return values[index];
    }
}

internal sealed record JsonArrayPropertyMatchSegment(string PropertyName, string ExpectedValue) : JsonPathSegment
{
    public override JsonElement Apply(JsonElement current)
    {
        if (current.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"JSON要素は配列ではありません: [{PropertyName}={ExpectedValue}]");
        }

        foreach (var item in current.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!TryGetProperty(item, PropertyName, out var property))
            {
                continue;
            }

            var value = property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : property.GetRawText();
            if (string.Equals(value, ExpectedValue, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        throw new InvalidOperationException($"条件に一致するJSON配列要素が見つかりません: {PropertyName}={ExpectedValue}");
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}

internal static class JsonPathParser
{
    public static IReadOnlyList<JsonPathSegment> Parse(string path)
    {
        var text = path.Trim();
        if (text.StartsWith('$'))
        {
            text = text[1..];
        }

        if (text.StartsWith('.'))
        {
            text = text[1..];
        }

        List<JsonPathSegment> segments = [];
        var index = 0;
        while (index < text.Length)
        {
            if (text[index] == '.')
            {
                index++;
                continue;
            }

            if (text[index] == '[')
            {
                var closeIndex = text.IndexOf(']', index + 1);
                if (closeIndex < 0)
                {
                    throw new FormatException("JSON抽出パスの ] が見つかりません。");
                }

                var selector = text[(index + 1)..closeIndex].Trim();
                segments.Add(CreateArraySegment(selector));
                index = closeIndex + 1;
                continue;
            }

            var nextIndex = index;
            while (nextIndex < text.Length && text[nextIndex] is not '.' and not '[')
            {
                nextIndex++;
            }

            var propertyName = text[index..nextIndex].Trim();
            if (propertyName.Length > 0)
            {
                segments.Add(new JsonPropertySegment(propertyName));
            }

            index = nextIndex;
        }

        if (segments.Count == 0)
        {
            throw new FormatException("JSON抽出パスが空です。");
        }

        return segments;
    }

    private static JsonPathSegment CreateArraySegment(string selector)
    {
        var equalsIndex = selector.IndexOf('=');
        if (equalsIndex > 0)
        {
            var propertyName = selector[..equalsIndex].Trim();
            var expectedValue = selector[(equalsIndex + 1)..].Trim().Trim('"', '\'');
            if (propertyName.Length == 0)
            {
                throw new FormatException("JSON配列条件のプロパティ名が空です。");
            }

            return new JsonArrayPropertyMatchSegment(propertyName, expectedValue);
        }

        return new JsonArrayIndexSegment(selector);
    }
}

internal static class VariableExpressionEvaluator
{
    public static double Evaluate(string expression, Func<string, double> resolveVariable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        ArgumentNullException.ThrowIfNull(resolveVariable);
        var parser = new Parser(expression, resolveVariable);
        return parser.Parse();
    }

    private sealed class Parser(string expression, Func<string, double> resolveVariable)
    {
        private readonly string _expression = expression;
        private readonly Func<string, double> _resolveVariable = resolveVariable;
        private int _index;

        public double Parse()
        {
            var value = ParseExpression();
            SkipWhiteSpace();
            if (_index != _expression.Length)
            {
                throw new FormatException($"計算式を解釈できません: {_expression[_index..]}");
            }

            return value;
        }

        private double ParseExpression()
        {
            var value = ParseTerm();
            while (true)
            {
                SkipWhiteSpace();
                if (Match('+'))
                {
                    value += ParseTerm();
                }
                else if (Match('-'))
                {
                    value -= ParseTerm();
                }
                else
                {
                    return value;
                }
            }
        }

        private double ParseTerm()
        {
            var value = ParseFactor();
            while (true)
            {
                SkipWhiteSpace();
                if (Match('*'))
                {
                    value *= ParseFactor();
                }
                else if (Match('/'))
                {
                    value /= ParseFactor();
                }
                else
                {
                    return value;
                }
            }
        }

        private double ParseFactor()
        {
            SkipWhiteSpace();
            if (Match('+'))
            {
                return ParseFactor();
            }

            if (Match('-'))
            {
                return -ParseFactor();
            }

            if (Match('('))
            {
                var value = ParseExpression();
                SkipWhiteSpace();
                if (!Match(')'))
                {
                    throw new FormatException("計算式の ) が見つかりません。");
                }

                return value;
            }

            return char.IsLetter(Current) || Current == '_'
                ? ParseVariable()
                : ParseNumber();
        }

        private double ParseVariable()
        {
            var start = _index;
            while (_index < _expression.Length && (char.IsLetterOrDigit(_expression[_index]) || _expression[_index] == '_'))
            {
                _index++;
            }

            return _resolveVariable(_expression[start.._index]);
        }

        private double ParseNumber()
        {
            var start = _index;
            while (_index < _expression.Length && (char.IsDigit(_expression[_index]) || _expression[_index] is '.' or 'e' or 'E' or '+' or '-'))
            {
                if (_index > start && (_expression[_index] is '+' or '-') && _expression[_index - 1] is not 'e' and not 'E')
                {
                    break;
                }

                _index++;
            }

            var text = _expression[start.._index];
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                throw new FormatException($"数値を解釈できません: {text}");
            }

            return value;
        }

        private char Current => _index < _expression.Length ? _expression[_index] : '\0';

        private bool Match(char expected)
        {
            if (Current != expected)
            {
                return false;
            }

            _index++;
            return true;
        }

        private void SkipWhiteSpace()
        {
            while (char.IsWhiteSpace(Current))
            {
                _index++;
            }
        }
    }
}

internal static class CsvRowBuilder
{
    public static string Build(string values, ICommandExecutionContext context)
    {
        var columns = SplitColumns(values)
            .Select(x => Escape(ResolveColumn(x.Trim(), context)));
        return string.Join(",", columns);
    }

    private static string ResolveColumn(string column, ICommandExecutionContext context)
    {
        if (column.Length == 0)
        {
            return string.Empty;
        }

        var variableValue = context.GetVariable(column);
        return variableValue ?? VariableTextResolver.Resolve(column, context);
    }

    private static IEnumerable<string> SplitColumns(string values)
    {
        return values
            .Replace("\r\n", ",", StringComparison.Ordinal)
            .Replace('\n', ',')
            .Split(',', StringSplitOptions.None);
    }

    private static string Escape(string value)
    {
        return value.Contains('"') || value.Contains(',') || value.Contains('\r') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
    }
}

internal static class VariableTextResolver
{
    public static string Resolve(string text, ICommandExecutionContext context)
    {
        var result = text;
        var startIndex = result.IndexOf('{', StringComparison.Ordinal);
        while (startIndex >= 0)
        {
            var endIndex = result.IndexOf('}', startIndex + 1);
            if (endIndex < 0)
            {
                break;
            }

            var variableName = result[(startIndex + 1)..endIndex];
            var value = context.GetVariable(variableName) ?? string.Empty;
            result = string.Concat(result.AsSpan(0, startIndex), value, result.AsSpan(endIndex + 1));
            startIndex = result.IndexOf("{", startIndex + value.Length, StringComparison.Ordinal);
        }

        return result;
    }
}

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.CalculateVariable, typeof(SimpleCommand), typeof(ICalculateVariableCommandSettings), CommandDef.CommandCategory.Variable, displayPriority: 6, displaySubPriority: 3, displayNameJa: "変数計算", displayNameEn: "Calculate Variable")]
    /// <summary>
    /// 変数を参照した四則演算を行い、結果を変数へ格納します。
    /// </summary>
    public partial class CalculateVariableItem : CommandListItem, ICalculateVariableItem, ICalculateVariableCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("計算式", EditorType.TextBox, Group = "変数計算", Order = 1,
                         Description = "例: (edgeX - edgeX0) * pixelSizeUm")]
        private string _expression = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("出力先変数名", EditorType.TextBox, Group = "変数計算", Order = 2,
                         Description = "計算結果を格納する変数名")]
        private string _outputVariableName = string.Empty;

        new public string Description => $"{OutputVariableName} = {Expression}";

        public CalculateVariableItem() { }

        public CalculateVariableItem(CalculateVariableItem? item = null) : base(item)
        {
            if (item is not null)
            {
                Expression = item.Expression;
                OutputVariableName = item.OutputVariableName;
            }
        }

        public new ICommandListItem Clone() => new CalculateVariableItem(this);

        public override ValueTask<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var value = VariableExpressionEvaluator.Evaluate(Expression, name =>
                {
                    var rawValue = context.GetVariable(name);
                    if (rawValue is null)
                    {
                        throw new InvalidOperationException($"変数 {name} が見つかりません。");
                    }

                    if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var invariantValue))
                    {
                        return invariantValue;
                    }

                    if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.CurrentCulture, out var currentValue))
                    {
                        return currentValue;
                    }

                    throw new FormatException($"変数 {name} の値を数値として解釈できません: {rawValue}");
                });

                var text = value.ToString("G17", CultureInfo.InvariantCulture);
                context.SetVariable(OutputVariableName, text);
                context.Log($"変数計算結果: {OutputVariableName} = {text}");
                return ValueTask.FromResult(true);
            }
            catch (Exception ex) when (ex is InvalidOperationException or FormatException)
            {
                context.Log($"変数計算エラー: {ex.Message}");
                return ValueTask.FromResult(false);
            }
        }
    }

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.AppendCsv, typeof(SimpleCommand), typeof(IAppendCsvCommandSettings), CommandDef.CommandCategory.Variable, displayPriority: 6, displaySubPriority: 4, displayNameJa: "CSV追記", displayNameEn: "Append CSV")]
    /// <summary>
    /// 変数値を CSV ファイルへ 1 行追記します。
    /// </summary>
    public partial class AppendCsvItem : CommandListItem, IAppendCsvItem, IAppendCsvCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("出力ファイルパス", EditorType.FilePicker, Group = "CSV出力", Order = 1,
                         Description = "追記先CSVファイル")]
        private string _outputFilePath = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ヘッダー行", EditorType.TextBox, Group = "CSV出力", Order = 2,
                         Description = "例: step,machineX,edgeX,edgeDelta,deltaUm")]
        private string _headerLine = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("追記する値", EditorType.TextBox, Group = "CSV出力", Order = 3,
                         Description = "変数名をカンマ区切りで指定。{varName} 形式の埋め込みも可能")]
        private string _values = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("初回のみヘッダーを書く", EditorType.CheckBox, Group = "CSV出力", Order = 4,
                         Description = "ファイルが空のときだけヘッダーを書き込みます")]
        private bool _writeHeaderOnce = true;

        new public string Description => $"CSV:{OutputFilePath} / 値:{Values}";

        public AppendCsvItem() { }

        public AppendCsvItem(AppendCsvItem? item = null) : base(item)
        {
            if (item is not null)
            {
                OutputFilePath = item.OutputFilePath;
                HeaderLine = item.HeaderLine;
                Values = item.Values;
                WriteHeaderOnce = item.WriteHeaderOnce;
            }
        }

        public new ICommandListItem Clone() => new AppendCsvItem(this);

        public override async ValueTask<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var resolvedPath = VariableTextResolver.Resolve(OutputFilePath, context);
                var absolutePath = context.ToAbsolutePath(resolvedPath);
                var directoryPath = Path.GetDirectoryName(absolutePath);
                if (!string.IsNullOrWhiteSpace(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                var shouldWriteHeader = WriteHeaderOnce
                    && !string.IsNullOrWhiteSpace(HeaderLine)
                    && (!File.Exists(absolutePath) || new FileInfo(absolutePath).Length == 0);

                await using var stream = new FileStream(absolutePath, FileMode.Append, FileAccess.Write, FileShare.Read);
                await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: shouldWriteHeader));
                if (shouldWriteHeader)
                {
                    await writer.WriteLineAsync(HeaderLine).ConfigureAwait(false);
                }

                var row = CsvRowBuilder.Build(Values, context);
                await writer.WriteLineAsync(row).ConfigureAwait(false);
                context.Log($"CSVへ追記しました: {absolutePath}");
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                context.Log($"CSV追記エラー: {ex.Message}");
                return false;
            }
        }
    }

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.SetVariableAI, typeof(SimpleCommand), typeof(ISetVariableAICommandSettings), CommandDef.CommandCategory.Variable, displayPriority: 6, displaySubPriority: 5, displayNameJa: "AI変数設定", displayNameEn: "Set AI Variable")]
    /// <summary>
    /// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
    /// </summary>
    public partial class SetVariableAIItem : CommandListItem, ISetVariableAIItem, ISetVariableAICommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ウィンドウタイトル", EditorType.WindowInfo, Group = "対象ウィンドウ", Order = 1,
                         Description = "操作対象のウィンドウタイトル（空欄で全画面）")]
        private string _windowTitle = string.Empty;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("検出モード", EditorType.ComboBox, Group = "AI設定", Order = 1,
                         Description = "取得する値の種類", Options = "Class,Count,X,Y,Width,Height")]
        private string _aIDetectMode = "Class";
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ウィンドウクラス名", EditorType.TextBox, Group = "対象ウィンドウ", Order = 2,
                         Description = "ウィンドウのクラス名")]
        private string _windowClassName = string.Empty;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ONNXモデル", EditorType.FilePicker, Group = "AI設定", Order = 2,
                         Description = "YOLOv8 ONNXモデルファイル")]
        private string _modelPath = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ラベルファイル", EditorType.FilePicker, Group = "AI設定", Order = 3,
                         Description = "未指定時はモデルmetadataと同階層のラベルファイルを利用")]
        private string _labelsPath = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ラベル名", EditorType.ComboBox, Group = "AI設定", Order = 4,
                         Description = "指定時は該当ラベルの検出結果を優先します")]
        private string _labelName = string.Empty;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("信頼度しきい値", EditorType.Slider, Group = "AI設定", Order = 5,
                         Description = "検出の信頼度しきい値", Min = 0.01, Max = 1.0, Step = 0.01)]
        private double _confThreshold = 0.5;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("IoUしきい値", EditorType.Slider, Group = "AI設定", Order = 6,
                         Description = "重なり除去のしきい値", Min = 0.01, Max = 1.0, Step = 0.01)]
        private double _ioUThreshold = 0.25;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("変数名", EditorType.TextBox, Group = "変数設定", Order = 1,
                         Description = "結果を格納する変数名")]
        private string _name = string.Empty;

        new public string Description =>
            $"変数:{Name} / モード:{AIDetectMode} / モデル:{System.IO.Path.GetFileName(ModelPath)} / {(string.IsNullOrWhiteSpace(LabelName) ? "全ラベル" : $"ラベル:{LabelName}")} / 閾値:C{ConfThreshold}/I{IoUThreshold}";

        public SetVariableAIItem() { }
        public SetVariableAIItem(SetVariableAIItem? item = null) : base(item)
        {
            if (item is not null)
            {
                WindowTitle = item.WindowTitle;
                WindowClassName = item.WindowClassName;
                AIDetectMode = item.AIDetectMode;
                ModelPath = item.ModelPath;
                LabelsPath = item.LabelsPath;
                LabelName = item.LabelName;
                ConfThreshold = item.ConfThreshold;
                IoUThreshold = item.IoUThreshold;
                Name = item.Name;
            }
        }
        public new ICommandListItem Clone()
        {
            return new SetVariableAIItem(this);
        }
        
        public override ValueTask<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            var absoluteModelPath = context.ToAbsolutePath(ModelPath);
            var absoluteLabelsPath = string.IsNullOrWhiteSpace(LabelsPath) ? string.Empty : context.ToAbsolutePath(LabelsPath);
            context.InitializeAIModel(absoluteModelPath, 640, true);

            var detections = context.DetectAI(WindowTitle, (float)ConfThreshold, (float)IoUThreshold);
            int? targetClassId = string.IsNullOrWhiteSpace(LabelName)
                ? null
                : context.ResolveAiClassId(absoluteModelPath, -1, LabelName, absoluteLabelsPath);
            var scopedDetections = targetClassId is { } classId
                ? detections.Where(d => d.ClassId == classId).ToList()
                : detections.ToList();

            string value = AIDetectMode switch
            {
                "Class" => scopedDetections.Count > 0 ? scopedDetections[0].ClassId.ToString() : "-1",
                "Count" => scopedDetections.Count.ToString(),
                "X" => scopedDetections.Count > 0 ? (scopedDetections[0].Rect.X + scopedDetections[0].Rect.Width / 2).ToString() : "-1",
                "Y" => scopedDetections.Count > 0 ? (scopedDetections[0].Rect.Y + scopedDetections[0].Rect.Height / 2).ToString() : "-1",
                "Width" => scopedDetections.Count > 0 ? scopedDetections[0].Rect.Width.ToString() : "-1",
                "Height" => scopedDetections.Count > 0 ? scopedDetections[0].Rect.Height.ToString() : "-1",
                _ => "0",
            };

            context.SetVariable(Name, value);
            var labelSuffix = string.IsNullOrWhiteSpace(LabelName) ? string.Empty : $" / ラベル: {LabelName}";
            context.Log($"AI検出結果: {Name} = {value} (モード: {AIDetectMode}, 対象検出数: {scopedDetections.Count}{labelSuffix})");
            return ValueTask.FromResult(true);
        }
    }

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.SetVariableOCR, typeof(SimpleCommand), typeof(ISetVariableOCRCommandSettings), CommandDef.CommandCategory.Variable, displayPriority: 6, displaySubPriority: 2, displayNameJa: "OCR変数設定", displayNameEn: "Set OCR Variable")]
    /// <summary>
    /// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
    /// </summary>
    public partial class SetVariableOCRItem : CommandListItem, ISetVariableOCRItem, ISetVariableOCRCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("変数名", EditorType.TextBox, Group = "変数設定", Order = 1, Description = "結果を格納する変数名")]
        private string _name = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("領域", EditorType.PointPicker, Group = "OCR領域", Order = 1, Description = "PickでOCR領域をドラッグ選択")]
        private int _x = 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("Y", EditorType.NumberBox, Group = "OCR領域", Order = 2, Description = "OCR領域の左上Y座標", Min = 0)]
        private int _y = 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("幅", EditorType.NumberBox, Group = "OCR領域", Order = 3, Description = "OCR領域の幅", Min = 1)]
        private int _width = 300;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("高さ", EditorType.NumberBox, Group = "OCR領域", Order = 4, Description = "OCR領域の高さ", Min = 1)]
        private int _height = 100;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ウィンドウタイトル", EditorType.WindowInfo, Group = "対象ウィンドウ", Order = 1, Description = "操作対象のウィンドウタイトル（空欄で全画面）")]
        private string _windowTitle = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ウィンドウクラス名", EditorType.TextBox, Group = "対象ウィンドウ", Order = 2, Description = "ウィンドウのクラス名")]
        private string _windowClassName = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("言語", EditorType.ComboBox, Group = "OCR設定", Order = 1, Description = "Tesseract OCRの言語", Options = "jpn,jpn+eng,eng")]
        private string _language = "jpn";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("PSM", EditorType.ComboBox, Group = "OCR設定", Order = 2, Description = "ページ分割モード", Options = "6,7,11,12,13")]
        private string _pageSegmentationMode = "6";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("最小信頼度", EditorType.Slider, Group = "OCR設定", Order = 3, Description = "この値未満なら空文字を保存", Min = 0.0, Max = 100.0, Step = 1.0)]
        private double _minConfidence = 50.0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("前処理", EditorType.ComboBox, Group = "OCR設定", Order = 4, Description = "OCR前の画像前処理", Options = "Gray,Binarize,AdaptiveThreshold,None")]
        private string _preprocessMode = "Gray";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("文字種制限", EditorType.TextBox, Group = "OCR設定", Order = 5, Description = "空欄で無効。例: 0123456789")]
        private string _whitelist = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("tessdataディレクトリ", EditorType.DirectoryPicker, Group = "詳細設定", Order = 1, Description = "必要な場合のみ指定")]
        private string _tessdataPath = string.Empty;

        new public string Description =>
            $"変数:{Name} / 領域:({X},{Y},{Width},{Height}) / 言語:{Language} / PSM:{PageSegmentationMode} / 最小信頼度:{MinConfidence:F0}";

        public SetVariableOCRItem() { }

        public SetVariableOCRItem(SetVariableOCRItem? item = null) : base(item)
        {
            if (item is not null)
            {
                Name = item.Name;
                X = item.X;
                Y = item.Y;
                Width = item.Width;
                Height = item.Height;
                WindowTitle = item.WindowTitle;
                WindowClassName = item.WindowClassName;
                Language = item.Language;
                PageSegmentationMode = item.PageSegmentationMode;
                Whitelist = item.Whitelist;
                MinConfidence = item.MinConfidence;
                PreprocessMode = item.PreprocessMode;
                TessdataPath = item.TessdataPath;
            }
        }

        public new ICommandListItem Clone() => new SetVariableOCRItem(this);

        public override async ValueTask<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            try
            {
                var result = await context.ExtractTextAsync(new OcrRequest
                {
                    X = X,
                    Y = Y,
                    Width = Width,
                    Height = Height,
                    WindowTitle = WindowTitle,
                    WindowClassName = WindowClassName,
                    Language = Language,
                    PageSegmentationMode = PageSegmentationMode,
                    Whitelist = Whitelist,
                    PreprocessMode = PreprocessMode,
                    TessdataPath = string.IsNullOrWhiteSpace(TessdataPath)
                        ? TessdataPath
                        : context.ToAbsolutePath(TessdataPath)
                }, cancellationToken).ConfigureAwait(false);

                var value = result.Confidence >= MinConfidence ? result.Text : string.Empty;
                context.SetVariable(Name, value);
                context.Log($"OCR結果: {Name} = \"{value}\" (信頼度: {result.Confidence:F1})");
                return true;
            }
            catch (Exception ex)
            {
                context.Log($"OCRエラー: {ex.Message}");
                return false;
            }
        }
    }


    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.IfVariable, typeof(IfVariableCommand), typeof(IIfVariableCommandSettings), CommandDef.CommandCategory.Condition, isIfCommand: true, displayPriority: 4, displaySubPriority: 5, displayNameJa: "変数比較", displayNameEn: "If Variable")]
    /// <summary>
    /// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
    /// </summary>
    public partial class IfVariableItem : CommandListItem, IIfVariableItem, IIfVariableCommandSettings, IIfItem
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("変数名", EditorType.TextBox, Group = "条件設定", Order = 1,
                         Description = "比較する変数の名前")]
        private string _name = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("演算子", EditorType.ComboBox, Group = "条件設定", Order = 2,
                         Description = "比較演算子", Options = "==,!=,>,<,>=,<=")]
        private string _operator = "==";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("比較値", EditorType.TextBox, Group = "条件設定", Order = 3,
                         Description = "比較する値")]
        private string _value = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private ICommandListItem? _pair = null;

        new public string Description => $"{LineNumber}->{Pair?.LineNumber} / If {Name} {Operator} \"{Value}\"";

        public IfVariableItem() { }
        public IfVariableItem(IfVariableItem? item = null) : base(item)
        {
            if (item is not null)
            {
                Name = item.Name;
                Operator = item.Operator;
                Value = item.Value;
                Pair = item.Pair;
            }
        }

        public new ICommandListItem Clone() => new IfVariableItem(this);
    }

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.Screenshot, typeof(SimpleCommand), typeof(IScreenshotCommandSettings), CommandDef.CommandCategory.System, displayPriority: 7, displaySubPriority: 2, displayNameJa: "スクリーンショット", displayNameEn: "Screenshot")]
    /// <summary>
    /// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
    /// </summary>
    public partial class ScreenshotItem : CommandListItem, IScreenshotItem, IScreenshotCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("保存先ディレクトリ", EditorType.DirectoryPicker, Group = "保存設定", Order = 1,
                         Description = "スクリーンショットの保存先")]
        private string _saveDirectory = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ウィンドウタイトル", EditorType.WindowInfo, Group = "対象ウィンドウ", Order = 1,
                         Description = "キャプチャ対象のウィンドウ（空欄で全画面）")]
        private string _windowTitle = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ウィンドウクラス名", EditorType.TextBox, Group = "対象ウィンドウ", Order = 2,
                         Description = "ウィンドウのクラス名")]
        private string _windowClassName = string.Empty;

        new public string Description =>
            $"対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "全画面" : $"{WindowTitle}[{WindowClassName}]")} / 保存先:{(string.IsNullOrEmpty(SaveDirectory) ? "(./Screenshots)" : SaveDirectory)}";

        public ScreenshotItem() { }
        public ScreenshotItem(ScreenshotItem? item = null) : base(item)
        {
            if (item is not null)
            {
                SaveDirectory = item.SaveDirectory;
                WindowTitle = item.WindowTitle;
                WindowClassName = item.WindowClassName;
            }
        }

        public new ICommandListItem Clone() => new ScreenshotItem(this);
        
        public override async ValueTask<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            try
            {
                var dir = string.IsNullOrWhiteSpace(SaveDirectory)
                    ? System.IO.Path.Combine(Environment.CurrentDirectory, "Screenshots")
                    : SaveDirectory;

                if (!System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }

                var fileName = $"screenshot_{context.GetLocalNow():yyyyMMdd_HHmmss}.png";
                var filePath = System.IO.Path.Combine(dir, fileName);

                await context.TakeScreenshotAsync(filePath, WindowTitle, WindowClassName, cancellationToken).ConfigureAwait(false);
                
                context.Log($"スクリーンショットを保存しました: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                context.Log($"スクリーンショットの保存に失敗しました: {ex.Message}");
                return false;
            }
        }
    }

    [CommandDef.CommandDefinition(CommandDef.CommandTypeNames.ClickImageAI, typeof(SimpleCommand), typeof(IClickImageAICommandSettings), CommandDef.CommandCategory.Click, displayPriority: 1, displaySubPriority: 3, displayNameJa: "画像クリック(AI検出)", displayNameEn: "AI Click")]
    /// <summary>
    /// コマンド一覧の 1 行として必要な表示情報と設定値を保持し、編集・実行の両方で利用できるようにします。
    /// </summary>
    public partial class ClickImageAIItem : CommandListItem, IClickImageAIItem, IClickImageAICommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ウィンドウタイトル", EditorType.WindowInfo, Group = "対象ウィンドウ", Order = 1,
                         Description = "操作対象のウィンドウタイトル（空欄で全画面）")]
        private string _windowTitle = string.Empty;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ウィンドウクラス名", EditorType.TextBox, Group = "対象ウィンドウ", Order = 2,
                         Description = "ウィンドウのクラス名")]
        private string _windowClassName = string.Empty;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ONNXモデル", EditorType.FilePicker, Group = "AI設定", Order = 1,
                         Description = "YOLOv8 ONNXモデルファイル")]
        private string _modelPath = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ラベルファイル", EditorType.FilePicker, Group = "AI設定", Order = 2,
                         Description = "未指定時はモデルmetadataと同階層のラベルファイルを利用")]
        private string _labelsPath = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("ラベル名", EditorType.ComboBox, Group = "AI設定", Order = 3,
                         Description = "選択時はクラスIDより優先して一致判定")]
        private string _labelName = string.Empty;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("クラスID", EditorType.NumberBox, Group = "AI設定", Order = 4,
                         Description = "検出する物体のクラス番号", Min = 0)]
        private int _classID = 0;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("信頼度しきい値", EditorType.Slider, Group = "AI設定", Order = 3,
                         Description = "検出の信頼度しきい値", Min = 0.01, Max = 1.0, Step = 0.01)]
        private double _confThreshold = 0.5;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("IoUしきい値", EditorType.Slider, Group = "AI設定", Order = 4,
                         Description = "重なり除去のしきい値", Min = 0.01, Max = 1.0, Step = 0.01)]
        private double _ioUThreshold = 0.25;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("マウスボタン", EditorType.MouseButtonPicker, Group = "クリック設定", Order = 1,
                         Description = "クリックに使用するボタン")]
        private CommandMouseButton _button = CommandMouseButton.Left;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("押下維持時間", EditorType.NumberBox, Group = "クリック設定", Order = 2,
                         Description = "マウス押下から離すまでの待機時間", Unit = "ミリ秒", Min = 0)]
        private int _holdDurationMs = 20;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("注入方式", EditorType.ComboBox, Group = "クリック設定", Order = 3,
                         Description = "クリック入力の送信方式", Options = "MouseEvent,SendInput")]
        private string _clickInjectionMode = "MouseEvent";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("移動シミュレート", EditorType.CheckBox, Group = "クリック設定", Order = 4,
                         Description = "クリック前にマウス移動を段階的にシミュレートする")]
        private bool _simulateMouseMove = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("クリック後に元の位置へ戻す", EditorType.CheckBox, Group = "クリック設定", Order = 5,
                         Description = "クリック完了後、実行前のカーソル位置へ戻します")]
        private bool _restoreCursorPositionAfterClick = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        [property: CommandProperty("クリック後にウィンドウ順を戻す", EditorType.CheckBox, Group = "クリック設定", Order = 6,
                         Description = "対象ウィンドウを前面化したあと、クリック完了時に元の重なり順へ戻します")]
        private bool _restoreWindowZOrderAfterClick = false;

        new public string Description =>
            $"対象：{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]")} / モデル:{System.IO.Path.GetFileName(ModelPath)} / {(string.IsNullOrWhiteSpace(LabelName) ? $"クラスID:{ClassID}" : $"ラベル:{LabelName}")} / 閾値:{ConfThreshold} / ボタン:{Button} / 押下維持:{HoldDurationMs}ms / 方式:{ClickInjectionMode} / 移動シミュレート:{(SimulateMouseMove ? "ON" : "OFF")} / 元位置へ戻す:{(RestoreCursorPositionAfterClick ? "ON" : "OFF")} / ウィンドウ順を戻す:{(RestoreWindowZOrderAfterClick ? "ON" : "OFF")}";

        public ClickImageAIItem() { }
        public ClickImageAIItem(ClickImageAIItem? item = null) : base(item)
        {
            if (item is not null)
            {
                WindowTitle = item.WindowTitle;
                WindowClassName = item.WindowClassName;
                ModelPath = item.ModelPath;
                LabelsPath = item.LabelsPath;
                LabelName = item.LabelName;
                ClassID = item.ClassID;
                ConfThreshold = item.ConfThreshold;
                IoUThreshold = item.IoUThreshold;
                Button = item.Button;
                HoldDurationMs = item.HoldDurationMs;
                ClickInjectionMode = item.ClickInjectionMode;
                SimulateMouseMove = item.SimulateMouseMove;
                RestoreCursorPositionAfterClick = item.RestoreCursorPositionAfterClick;
                RestoreWindowZOrderAfterClick = item.RestoreWindowZOrderAfterClick;
            }
        }

        public new ICommandListItem Clone() => new ClickImageAIItem(this);
        
        public override async ValueTask<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var absoluteModelPath = context.ToAbsolutePath(ModelPath);
            var absoluteLabelsPath = string.IsNullOrWhiteSpace(LabelsPath) ? string.Empty : context.ToAbsolutePath(LabelsPath);
            context.InitializeAIModel(absoluteModelPath, 640, true);
            var targetClassId = context.ResolveAiClassId(absoluteModelPath, ClassID, LabelName, absoluteLabelsPath);

            var detections = context.DetectAI(WindowTitle, (float)ConfThreshold, (float)IoUThreshold);
            var targetDetections = detections.Where(d => d.ClassId == targetClassId).ToList();

            if (targetDetections.Count > 0)
            {
                var best = targetDetections.OrderByDescending(d => d.Score).First();
                int centerX = best.Rect.X + best.Rect.Width / 2;
                int centerY = best.Rect.Y + best.Rect.Height / 2;

                cancellationToken.ThrowIfCancellationRequested();
                await context.ClickAsync(centerX, centerY, Button, WindowTitle, WindowClassName, HoldDurationMs, ClickInjectionMode, SimulateMouseMove, RestoreCursorPositionAfterClick, RestoreWindowZOrderAfterClick).ConfigureAwait(false);
                var labelSuffix = string.IsNullOrWhiteSpace(LabelName) ? string.Empty : $" / ラベル: {LabelName}";
                context.Log($"AI画像をクリックしました。({centerX}, {centerY}) / クラスID: {best.ClassId}{labelSuffix} / スコア: {best.Score:F2}");
                return true;
            }

            var missingLabelSuffix = string.IsNullOrWhiteSpace(LabelName) ? string.Empty : $" / ラベル: {LabelName}";
            context.Log($"クラスID {targetClassId}{missingLabelSuffix} の画像が見つかりませんでした。");
            return false;
        }
    }

