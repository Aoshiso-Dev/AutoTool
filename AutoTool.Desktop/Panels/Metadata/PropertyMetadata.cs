using System.Collections.Concurrent;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Media;
using AutoTool.Commands.Model.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoTool.Automation.Runtime.Attributes;

/// <summary>
/// 反射で取得したコマンドプロパティを UI エディタで表示・編集するためのメタデータです。
/// </summary>
public partial class PropertyMetadata : ObservableObject
{
    private static readonly ConcurrentDictionary<Type, ModifierPropertyCache> ModifierPropertyCacheByType = new();

    public PropertyInfo PropertyInfo { get; init; } = null!;
    public CommandPropertyAttribute Attribute { get; init; } = null!;
    public object Target { get; init; } = null!;

    public string DisplayName => Attribute.DisplayName;
    public string? Description => Attribute.Description;
    public string Group => Attribute.Group;
    public int Order => Attribute.Order;
    public EditorType EditorType => Attribute.EditorType;
    public double Min => Attribute.Min;
    public double Max => Attribute.Max;
    public double Step => Attribute.Step;
    public string? Unit => Attribute.Unit;
    public string[]? Options => DynamicOptions ?? Attribute.Options?.Split(',').Select(s => s.Trim()).ToArray();
    public bool HasOptions => Options is { Length: > 0 };
    public string? FileFilter => Attribute.FileFilter;
    public Type PropertyType => PropertyInfo.PropertyType;

    [ObservableProperty]
    private ICommand? _browseCommand;

    [ObservableProperty]
    private ICommand? _captureCommand;

    [ObservableProperty]
    private ICommand? _pickColorCommand;

    [ObservableProperty]
    private ICommand? _getWindowInfoCommand;

    [ObservableProperty]
    private ICommand? _clearCommand;

    [ObservableProperty]
    private ICommand? _openReferenceCommand;

    [ObservableProperty]
    private ICommand? _downloadRecommendedCommand;

    [ObservableProperty]
    private string? _helperText;

    [ObservableProperty]
    private string? _validationMessage;

    [ObservableProperty]
    private bool _hasValidationError;

    [ObservableProperty]
    private string[]? _dynamicOptions;

    partial void OnDynamicOptionsChanged(string[]? value)
    {
        OnPropertyChanged(nameof(Options));
        OnPropertyChanged(nameof(HasOptions));
    }

    public object? Value
    {
        get => PropertyInfo.GetValue(Target);
        set
        {
            var currentValue = PropertyInfo.GetValue(Target);
            if (!Equals(currentValue, value))
            {
                PropertyInfo.SetValue(Target, value);
                NotifyAllValueProperties();
            }
        }
    }

    public void NotifyAllValueProperties()
    {
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(StringValue));
        OnPropertyChanged(nameof(IntValue));
        OnPropertyChanged(nameof(DoubleValue));
        OnPropertyChanged(nameof(BoolValue));
        OnPropertyChanged(nameof(MouseButtonValue));
        OnPropertyChanged(nameof(ColorValue));
        OnPropertyChanged(nameof(ColorBrush));
        OnPropertyChanged(nameof(HasColor));
        OnPropertyChanged(nameof(KeyValue));
        OnPropertyChanged(nameof(KeyDisplayText));
        OnPropertyChanged(nameof(HelperText));
        OnPropertyChanged(nameof(Options));
        OnPropertyChanged(nameof(HasOptions));
        OnPropertyChanged(nameof(ValidationMessage));
        OnPropertyChanged(nameof(HasValidationError));
    }

    public string StringValue
    {
        get => Value?.ToString() ?? string.Empty;
        set => Value = value;
    }

    public int IntValue
    {
        get => Value is int i ? i : 0;
        set => Value = value;
    }

    public double DoubleValue
    {
        get => Value is double d ? d : 0.0;
        set => Value = value;
    }

    public bool BoolValue
    {
        get => Value is bool b && b;
        set => Value = value;
    }

    public CommandMouseButton[] MouseButtonOptions { get; } = Enum.GetValues<CommandMouseButton>();

    public CommandMouseButton MouseButtonValue
    {
        get => Value is CommandMouseButton mb ? mb : CommandMouseButton.Left;
        set
        {
            Value = value;
            OnPropertyChanged();
        }
    }

    public Color? ColorValue
    {
        get => Value switch
        {
            CommandColor c => Color.FromArgb(c.A, c.R, c.G, c.B),
            Color c => c,
            _ => null
        };
        set => Value = value.HasValue ? new CommandColor(value.Value.A, value.Value.R, value.Value.G, value.Value.B) : null;
    }

    public SolidColorBrush? ColorBrush =>
        ColorValue.HasValue ? new SolidColorBrush(ColorValue.Value) : null;

    public bool HasColor => ColorValue.HasValue;

    public CommandKey KeyValue
    {
        get => Value is CommandKey k ? k : CommandKey.None;
        set => Value = value;
    }

    public string KeyDisplayText
    {
        get
        {
            if (KeyValue == CommandKey.None)
            {
                return "None";
            }

            var cache = ModifierPropertyCacheByType.GetOrAdd(Target.GetType(), static type =>
                new ModifierPropertyCache(
                    GetBooleanProperty(type, "Ctrl"),
                    GetBooleanProperty(type, "Alt"),
                    GetBooleanProperty(type, "Shift")));

            var ctrl = cache.Ctrl?.GetValue(Target) as bool? ?? false;
            var alt = cache.Alt?.GetValue(Target) as bool? ?? false;
            var shift = cache.Shift?.GetValue(Target) as bool? ?? false;

            List<string> keys = [];
            if (ctrl) keys.Add("Ctrl");
            if (alt) keys.Add("Alt");
            if (shift) keys.Add("Shift");
            keys.Add(KeyValue.ToString());

            return string.Join(" + ", keys);
        }
    }

    [ObservableProperty]
    private ICommand? _pickKeyCommand;

    [ObservableProperty]
    private ICommand? _pickPointCommand;

    [ObservableProperty]
    private PropertyMetadata? _relatedProperty;

    [ObservableProperty]
    private PropertyMetadata? _relatedProperty2;

    [ObservableProperty]
    private PropertyMetadata? _relatedProperty3;

    public int RelatedIntValue => RelatedProperty?.IntValue ?? 0;
    public int RelatedIntValue2 => RelatedProperty2?.IntValue ?? 0;
    public int RelatedIntValue3 => RelatedProperty3?.IntValue ?? 0;

    public void NotifyRelatedValueChanged()
    {
        NotifyAllValueProperties();
        OnPropertyChanged(nameof(RelatedIntValue));
        OnPropertyChanged(nameof(RelatedIntValue2));
        OnPropertyChanged(nameof(RelatedIntValue3));
        RelatedProperty?.NotifyAllValueProperties();
        RelatedProperty2?.NotifyAllValueProperties();
        RelatedProperty3?.NotifyAllValueProperties();
    }

    private static PropertyInfo? GetBooleanProperty(Type targetType, string propertyName)
    {
        var property = targetType.GetProperty(propertyName);
        return property?.PropertyType == typeof(bool) ? property : null;
    }

    /// <summary>
    /// 不変前提で扱うデータをまとめ、比較やコピーを安全に行えるようにします。
    /// </summary>

    private sealed record ModifierPropertyCache(PropertyInfo? Ctrl, PropertyInfo? Alt, PropertyInfo? Shift);
}

/// <summary>
/// プロパティエディタで表示する項目をグループ化し、画面上で関連設定をまとめて扱えるようにします。
/// </summary>
public class PropertyGroup
{
    public string GroupName { get; init; } = string.Empty;
    public List<PropertyMetadata> Properties { get; init; } = [];
}
