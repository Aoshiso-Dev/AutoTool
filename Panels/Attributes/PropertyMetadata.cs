using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MacroPanels.Attributes;

/// <summary>
/// プロパティのメタデータ（UIで使用）
/// </summary>
public partial class PropertyMetadata : ObservableObject
{
    /// <summary>プロパティ情報</summary>
    public PropertyInfo PropertyInfo { get; init; } = null!;
    
    /// <summary>属性情報</summary>
    public CommandPropertyAttribute Attribute { get; init; } = null!;
    
    /// <summary>対象オブジェクト</summary>
    public object Target { get; init; } = null!;
    
    /// <summary>表示名</summary>
    public string DisplayName => Attribute.DisplayName;
    
    /// <summary>説明</summary>
    public string? Description => Attribute.Description;
    
    /// <summary>グループ名</summary>
    public string Group => Attribute.Group;
    
    /// <summary>順序</summary>
    public int Order => Attribute.Order;
    
    /// <summary>エディタ種類</summary>
    public EditorType EditorType => Attribute.EditorType;
    
    /// <summary>最小値</summary>
    public double Min => Attribute.Min;
    
    /// <summary>最大値</summary>
    public double Max => Attribute.Max;
    
    /// <summary>ステップ</summary>
    public double Step => Attribute.Step;
    
    /// <summary>単位</summary>
    public string? Unit => Attribute.Unit;
    
    /// <summary>選択肢</summary>
    public string[]? Options => Attribute.Options?.Split(',').Select(s => s.Trim()).ToArray();
    
    /// <summary>ファイルフィルター</summary>
    public string? FileFilter => Attribute.FileFilter;
    
    /// <summary>プロパティの型</summary>
    public Type PropertyType => PropertyInfo.PropertyType;
    
    // ボタンアクション用コマンド
    private ICommand? _browseCommand;
    private ICommand? _captureCommand;
    private ICommand? _pickColorCommand;
    private ICommand? _getWindowInfoCommand;
    private ICommand? _clearCommand;
    
    /// <summary>参照ボタンコマンド</summary>
    public ICommand? BrowseCommand 
    { 
        get => _browseCommand;
        set => SetProperty(ref _browseCommand, value);
    }
    
    /// <summary>キャプチャボタンコマンド</summary>
    public ICommand? CaptureCommand 
    { 
        get => _captureCommand;
        set => SetProperty(ref _captureCommand, value);
    }
    
    /// <summary>色取得ボタンコマンド</summary>
    public ICommand? PickColorCommand 
    { 
        get => _pickColorCommand;
        set => SetProperty(ref _pickColorCommand, value);
    }
    
    /// <summary>ウィンドウ情報取得コマンド</summary>
    public ICommand? GetWindowInfoCommand 
    { 
        get => _getWindowInfoCommand;
        set => SetProperty(ref _getWindowInfoCommand, value);
    }
    
    /// <summary>クリアコマンド</summary>
    public ICommand? ClearCommand 
    { 
        get => _clearCommand;
        set => SetProperty(ref _clearCommand, value);
    }
    
    
    /// <summary>
    /// 現在の値を取得・設定
    /// </summary>
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
    
    /// <summary>
    /// 全ての値関連プロパティの変更を通知
    /// </summary>
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
    
    /// <summary>
    /// 文字列型の値（TextBox用）
    /// </summary>
    public string StringValue
    {
        get => Value?.ToString() ?? string.Empty;
        set => Value = value;
    }
    
    /// <summary>
    /// 整数型の値（NumberBox用）
    /// </summary>
    public int IntValue
    {
        get => Value is int i ? i : 0;
        set => Value = value;
    }
    
    /// <summary>
    /// 小数型の値（Slider用）
    /// </summary>
    public double DoubleValue
    {
        get => Value is double d ? d : 0.0;
        set => Value = value;
    }
    
    /// <summary>
    /// ブール型の値（CheckBox用）
    /// </summary>
    public bool BoolValue
    {
        get => Value is bool b && b;
        set => Value = value;
    }
    
    /// <summary>
    /// マウスボタンの選択肢
    /// </summary>
    public System.Windows.Input.MouseButton[] MouseButtonOptions { get; } = 
        Enum.GetValues<System.Windows.Input.MouseButton>();
    
    /// <summary>
    /// マウスボタンの値
    /// </summary>
    public System.Windows.Input.MouseButton MouseButtonValue
    {
        get => Value is System.Windows.Input.MouseButton mb ? mb : System.Windows.Input.MouseButton.Left;
        set 
        { 
            Value = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// カラーの値（ColorPicker用）
    /// </summary>
    public System.Windows.Media.Color? ColorValue
    {
        get => Value as System.Windows.Media.Color?;
        set => Value = value;
    }
    
    /// <summary>
    /// カラーのブラシ（表示用）
    /// </summary>
    public System.Windows.Media.SolidColorBrush? ColorBrush
    {
        get => ColorValue.HasValue 
            ? new System.Windows.Media.SolidColorBrush(ColorValue.Value) 
            : null;
    }
    
    /// <summary>
    /// カラーが設定されているか
    /// </summary>
    public bool HasColor => ColorValue.HasValue;
    
    /// <summary>
    /// キーの値（KeyPicker用）
    /// </summary>
    public System.Windows.Input.Key KeyValue
    {
        get => Value is System.Windows.Input.Key k ? k : System.Windows.Input.Key.None;
        set => Value = value;
    }
    
    /// <summary>
    /// キーの表示テキスト
    /// </summary>
    public string KeyDisplayText => KeyValue == System.Windows.Input.Key.None ? "None" : KeyValue.ToString();
    
    // キーピッカー用コマンド
    private ICommand? _pickKeyCommand;
    
    /// <summary>キー取得コマンド</summary>
    public ICommand? PickKeyCommand 
    { 
        get => _pickKeyCommand;
        set => SetProperty(ref _pickKeyCommand, value);
    }
    
    // ポイントピッカー用コマンド
    private ICommand? _pickPointCommand;
    
    /// <summary>座標取得コマンド</summary>
    public ICommand? PickPointCommand 
    { 
        get => _pickPointCommand;
        set => SetProperty(ref _pickPointCommand, value);
    }
    
    
    // 関連プロパティ（例：Yの値を参照）
    private PropertyMetadata? _relatedProperty;
    
    /// <summary>関連プロパティ（例：X座標に対するY座標）</summary>
    public PropertyMetadata? RelatedProperty 
    { 
        get => _relatedProperty;
        set => SetProperty(ref _relatedProperty, value);
    }
    
    /// <summary>関連プロパティの整数値</summary>
    public int RelatedIntValue => RelatedProperty?.IntValue ?? 0;
    
    /// <summary>関連値の更新を通知</summary>
    public void NotifyRelatedValueChanged()
    {
        NotifyAllValueProperties();
        OnPropertyChanged(nameof(RelatedIntValue));
        RelatedProperty?.NotifyAllValueProperties();
    }
}

/// <summary>
/// プロパティグループ（カード表示用）
/// </summary>
public class PropertyGroup
{
    /// <summary>グループ名</summary>
    public string GroupName { get; init; } = string.Empty;
    
    /// <summary>グループ内のプロパティ一覧</summary>
    public List<PropertyMetadata> Properties { get; init; } = new();
}
