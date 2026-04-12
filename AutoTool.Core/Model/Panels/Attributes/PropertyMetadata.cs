using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AutoTool.Panels.Attributes;

/// <summary>
/// �v���p�e�B�̃��^�f�[�^�iUI�Ŏg�p�j
/// </summary>
public partial class PropertyMetadata : ObservableObject
{
    /// <summary>�v���p�e�B���</summary>
    public PropertyInfo PropertyInfo { get; init; } = null!;
    
    /// <summary>�������</summary>
    public CommandPropertyAttribute Attribute { get; init; } = null!;
    
    /// <summary>�ΏۃI�u�W�F�N�g</summary>
    public object Target { get; init; } = null!;
    
    /// <summary>�\����</summary>
    public string DisplayName => Attribute.DisplayName;
    
    /// <summary>���</summary>
    public string? Description => Attribute.Description;
    
    /// <summary>�O���[�v��</summary>
    public string Group => Attribute.Group;
    
    /// <summary>����</summary>
    public int Order => Attribute.Order;
    
    /// <summary>�G�f�B�^���</summary>
    public EditorType EditorType => Attribute.EditorType;
    
    /// <summary>�ŏ��l</summary>
    public double Min => Attribute.Min;
    
    /// <summary>�ő�l</summary>
    public double Max => Attribute.Max;
    
    /// <summary>�X�e�b�v</summary>
    public double Step => Attribute.Step;
    
    /// <summary>�P��</summary>
    public string? Unit => Attribute.Unit;
    
    /// <summary>�I���</summary>
    public string[]? Options => Attribute.Options?.Split(',').Select(s => s.Trim()).ToArray();
    
    /// <summary>�t�@�C���t�B���^�[</summary>
    public string? FileFilter => Attribute.FileFilter;
    
    /// <summary>�v���p�e�B�̌^</summary>
    public Type PropertyType => PropertyInfo.PropertyType;
    
    // �{�^���A�N�V�����p�R�}���h
    private ICommand? _browseCommand;
    private ICommand? _captureCommand;
    private ICommand? _pickColorCommand;
    private ICommand? _getWindowInfoCommand;
    private ICommand? _clearCommand;
    
    /// <summary>�Q�ƃ{�^���R�}���h</summary>
    public ICommand? BrowseCommand 
    { 
        get => _browseCommand;
        set => SetProperty(ref _browseCommand, value);
    }
    
    /// <summary>�L���v�`���{�^���R�}���h</summary>
    public ICommand? CaptureCommand 
    { 
        get => _captureCommand;
        set => SetProperty(ref _captureCommand, value);
    }
    
    /// <summary>�F�擾�{�^���R�}���h</summary>
    public ICommand? PickColorCommand 
    { 
        get => _pickColorCommand;
        set => SetProperty(ref _pickColorCommand, value);
    }
    
    /// <summary>�E�B���h�E���擾�R�}���h</summary>
    public ICommand? GetWindowInfoCommand 
    { 
        get => _getWindowInfoCommand;
        set => SetProperty(ref _getWindowInfoCommand, value);
    }
    
    /// <summary>�N���A�R�}���h</summary>
    public ICommand? ClearCommand 
    { 
        get => _clearCommand;
        set => SetProperty(ref _clearCommand, value);
    }
    
    
    /// <summary>
    /// ���݂̒l��擾�E�ݒ�
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
    /// �S�Ă̒l�֘A�v���p�e�B�̕ύX��ʒm
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
    /// ������^�̒l�iTextBox�p�j
    /// </summary>
    public string StringValue
    {
        get => Value?.ToString() ?? string.Empty;
        set => Value = value;
    }
    
    /// <summary>
    /// �����^�̒l�iNumberBox�p�j
    /// </summary>
    public int IntValue
    {
        get => Value is int i ? i : 0;
        set => Value = value;
    }
    
    /// <summary>
    /// �����^�̒l�iSlider�p�j
    /// </summary>
    public double DoubleValue
    {
        get => Value is double d ? d : 0.0;
        set => Value = value;
    }
    
    /// <summary>
    /// �u�[���^�̒l�iCheckBox�p�j
    /// </summary>
    public bool BoolValue
    {
        get => Value is bool b && b;
        set => Value = value;
    }
    
    /// <summary>
    /// �}�E�X�{�^���̑I���
    /// </summary>
    public System.Windows.Input.MouseButton[] MouseButtonOptions { get; } = 
        Enum.GetValues<System.Windows.Input.MouseButton>();
    
    /// <summary>
    /// �}�E�X�{�^���̒l
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
    /// �J���[�̒l�iColorPicker�p�j
    /// </summary>
    public System.Windows.Media.Color? ColorValue
    {
        get => Value as System.Windows.Media.Color?;
        set => Value = value;
    }
    
    /// <summary>
    /// �J���[�̃u���V�i�\���p�j
    /// </summary>
    public System.Windows.Media.SolidColorBrush? ColorBrush
    {
        get => ColorValue.HasValue 
            ? new System.Windows.Media.SolidColorBrush(ColorValue.Value) 
            : null;
    }
    
    /// <summary>
    /// �J���[���ݒ肳��Ă��邩
    /// </summary>
    public bool HasColor => ColorValue.HasValue;
    
    /// <summary>
    /// �L�[�̒l�iKeyPicker�p�j
    /// </summary>
    public System.Windows.Input.Key KeyValue
    {
        get => Value is System.Windows.Input.Key k ? k : System.Windows.Input.Key.None;
        set => Value = value;
    }
    
    /// <summary>
    /// �L�[�̕\���e�L�X�g
    /// </summary>
    public string KeyDisplayText => KeyValue == System.Windows.Input.Key.None ? "None" : KeyValue.ToString();
    
    // �L�[�s�b�J�[�p�R�}���h
    private ICommand? _pickKeyCommand;
    
    /// <summary>�L�[�擾�R�}���h</summary>
    public ICommand? PickKeyCommand 
    { 
        get => _pickKeyCommand;
        set => SetProperty(ref _pickKeyCommand, value);
    }
    
    // �|�C���g�s�b�J�[�p�R�}���h
    private ICommand? _pickPointCommand;
    
    /// <summary>���W�擾�R�}���h</summary>
    public ICommand? PickPointCommand 
    { 
        get => _pickPointCommand;
        set => SetProperty(ref _pickPointCommand, value);
    }
    
    
    // �֘A�v���p�e�B�i��FY�̒l��Q�Ɓj
    private PropertyMetadata? _relatedProperty;
    
    /// <summary>�֘A�v���p�e�B�i��FX���W�ɑ΂���Y���W�j</summary>
    public PropertyMetadata? RelatedProperty 
    { 
        get => _relatedProperty;
        set => SetProperty(ref _relatedProperty, value);
    }
    
    /// <summary>�֘A�v���p�e�B�̐����l</summary>
    public int RelatedIntValue => RelatedProperty?.IntValue ?? 0;
    
    /// <summary>�֘A�l�̍X�V��ʒm</summary>
    public void NotifyRelatedValueChanged()
    {
        NotifyAllValueProperties();
        OnPropertyChanged(nameof(RelatedIntValue));
        RelatedProperty?.NotifyAllValueProperties();
    }
}

/// <summary>
/// �v���p�e�B�O���[�v�i�J�[�h�\���p�j
/// </summary>
public class PropertyGroup
{
    /// <summary>�O���[�v��</summary>
    public string GroupName { get; init; } = string.Empty;
    
    /// <summary>�O���[�v��̃v���p�e�B�ꗗ</summary>
    public List<PropertyMetadata> Properties { get; init; } = new();
}

