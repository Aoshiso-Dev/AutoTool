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

    public string? PropertyNameOverride { get; init; }
    public string? DisplayNameOverride { get; init; }
    public string? DescriptionOverride { get; init; }
    public string? GroupOverride { get; init; }
    public int? OrderOverride { get; init; }
    public EditorType? EditorTypeOverride { get; init; }
    public double? MinOverride { get; init; }
    public double? MaxOverride { get; init; }
    public double? StepOverride { get; init; }
    public string? UnitOverride { get; init; }
    public string[]? OptionsOverride { get; init; }
    public string? FileFilterOverride { get; init; }
    public Type? PropertyTypeOverride { get; init; }
    public Func<object?>? GetValueFunc { get; init; }
    public Action<object?>? SetValueAction { get; init; }

    public string PropertyName => PropertyNameOverride ?? PropertyInfo.Name;
    public string DisplayName => DisplayNameOverride ?? Attribute.DisplayName;
    public string? Description => DescriptionOverride ?? Attribute.Description;
    public string Group => GroupOverride ?? Attribute.Group;
    public int Order => OrderOverride ?? Attribute.Order;
    public EditorType EditorType => EditorTypeOverride ?? Attribute.EditorType;
    public double Min => MinOverride ?? Attribute.Min;
    public double Max => MaxOverride ?? Attribute.Max;
    public double Step => StepOverride ?? Attribute.Step;
    public string? Unit => UnitOverride ?? Attribute.Unit;
    public string[]? Options => DynamicOptions ?? OptionsOverride ?? Attribute.Options?.Split(',').Select(s => s.Trim()).ToArray();
    public bool HasOptions => Options is { Length: > 0 };
    public string? FileFilter => FileFilterOverride ?? Attribute.FileFilter;
    public Type PropertyType => PropertyTypeOverride ?? PropertyInfo.PropertyType;

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
        get => GetValueFunc?.Invoke() ?? PropertyInfo.GetValue(Target);
        set
        {
            var currentValue = Value;
            if (!Equals(currentValue, value))
            {
                if (SetValueAction is not null)
                {
                    SetValueAction(value);
                }
                else
                {
                    PropertyInfo.SetValue(Target, value);
                }
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

