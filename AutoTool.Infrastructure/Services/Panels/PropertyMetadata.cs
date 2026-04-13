using System.Collections.Concurrent;
using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoTool.Panels.Attributes;

/// <summary>
/// UI editor metadata for a reflected command property.
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
    public string[]? Options => Attribute.Options?.Split(',').Select(s => s.Trim()).ToArray();
    public string? FileFilter => Attribute.FileFilter;
    public Type PropertyType => PropertyInfo.PropertyType;

    private ICommand? _browseCommand;
    private ICommand? _captureCommand;
    private ICommand? _pickColorCommand;
    private ICommand? _getWindowInfoCommand;
    private ICommand? _clearCommand;

    public ICommand? BrowseCommand
    {
        get => _browseCommand;
        set => SetProperty(ref _browseCommand, value);
    }

    public ICommand? CaptureCommand
    {
        get => _captureCommand;
        set => SetProperty(ref _captureCommand, value);
    }

    public ICommand? PickColorCommand
    {
        get => _pickColorCommand;
        set => SetProperty(ref _pickColorCommand, value);
    }

    public ICommand? GetWindowInfoCommand
    {
        get => _getWindowInfoCommand;
        set => SetProperty(ref _getWindowInfoCommand, value);
    }

    public ICommand? ClearCommand
    {
        get => _clearCommand;
        set => SetProperty(ref _clearCommand, value);
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

    public MouseButton[] MouseButtonOptions { get; } = Enum.GetValues<MouseButton>();

    public MouseButton MouseButtonValue
    {
        get => Value is MouseButton mb ? mb : MouseButton.Left;
        set
        {
            Value = value;
            OnPropertyChanged();
        }
    }

    public System.Windows.Media.Color? ColorValue
    {
        get => Value as System.Windows.Media.Color?;
        set => Value = value;
    }

    public System.Windows.Media.SolidColorBrush? ColorBrush =>
        ColorValue.HasValue ? new System.Windows.Media.SolidColorBrush(ColorValue.Value) : null;

    public bool HasColor => ColorValue.HasValue;

    public Key KeyValue
    {
        get => Value is Key k ? k : Key.None;
        set => Value = value;
    }

    public string KeyDisplayText
    {
        get
        {
            if (KeyValue == Key.None)
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

            var keys = new List<string>();
            if (ctrl) keys.Add("Ctrl");
            if (alt) keys.Add("Alt");
            if (shift) keys.Add("Shift");
            keys.Add(KeyValue.ToString());

            return string.Join(" + ", keys);
        }
    }

    private ICommand? _pickKeyCommand;
    public ICommand? PickKeyCommand
    {
        get => _pickKeyCommand;
        set => SetProperty(ref _pickKeyCommand, value);
    }

    private ICommand? _pickPointCommand;
    public ICommand? PickPointCommand
    {
        get => _pickPointCommand;
        set => SetProperty(ref _pickPointCommand, value);
    }

    private PropertyMetadata? _relatedProperty;
    public PropertyMetadata? RelatedProperty
    {
        get => _relatedProperty;
        set => SetProperty(ref _relatedProperty, value);
    }

    public int RelatedIntValue => RelatedProperty?.IntValue ?? 0;

    public void NotifyRelatedValueChanged()
    {
        NotifyAllValueProperties();
        OnPropertyChanged(nameof(RelatedIntValue));
        RelatedProperty?.NotifyAllValueProperties();
    }

    private static PropertyInfo? GetBooleanProperty(Type targetType, string propertyName)
    {
        var property = targetType.GetProperty(propertyName);
        return property?.PropertyType == typeof(bool) ? property : null;
    }

    private sealed record ModifierPropertyCache(PropertyInfo? Ctrl, PropertyInfo? Alt, PropertyInfo? Shift);
}

public class PropertyGroup
{
    public string GroupName { get; init; } = string.Empty;
    public List<PropertyMetadata> Properties { get; init; } = new();
}
