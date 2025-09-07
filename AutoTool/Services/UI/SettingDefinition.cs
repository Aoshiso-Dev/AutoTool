using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoTool.Services.UI
{
    /*
    /// <summary>
    /// 設定コントロールの種類
    /// </summary>
    public enum SettingControlType
    {
        TextBox,
        NumberBox,
        CheckBox,
        ComboBox,
        Slider,
        FilePicker,
        FolderPicker,
        OnnxPicker,
        ColorPicker,
        DatePicker,
        TimePicker,
        KeyPicker,
        CoordinatePicker,
        WindowPicker,
        PasswordBox
    }

    /// <summary>
    /// コマンド設定定義（動的UI拡張版）
    /// </summary>
    public class SettingDefinition : INotifyPropertyChanged
    {
        private string _propertyName = string.Empty;
        private string _displayName = string.Empty;
        private Type _propertyType = typeof(string);
        private object? _defaultValue;
        private string _description = string.Empty;
        private bool _isRequired;
        private bool _isReadOnly;
        private object? _minValue;
        private object? _maxValue;
        private string? _sourceCollection;
        private string _category = "基本";
        private int _order;
        private string _editorType = "Text";
        private string? _validationRule;
        private SettingControlType _controlType = SettingControlType.TextBox; // XAML側の制御判定用
        private object? _currentValue;            // 現在値（表示用）
        private bool _showCurrentValue = true;    // 現在値表示フラグ
        private string? _unit;                    // 単位
        private ObservableCollection<object>? _sourceItems; // ComboBox等の候補
        private string? _fileFilter;              // FilePickerで使用

        public string PropertyName
        {
            get => _propertyName;
            set => SetField(ref _propertyName, value);
        }

        public string DisplayName
        {
            get => _displayName;
            set => SetField(ref _displayName, value);
        }

        public Type PropertyType
        {
            get => _propertyType;
            set => SetField(ref _propertyType, value);
        }

        public object? DefaultValue
        {
            get => _defaultValue;
            set => SetField(ref _defaultValue, value);
        }

        public string Description
        {
            get => _description;
            set => SetField(ref _description, value);
        }

        public bool IsRequired
        {
            get => _isRequired;
            set => SetField(ref _isRequired, value);
        }

        public bool IsReadOnly
        {
            get => _isReadOnly;
            set => SetField(ref _isReadOnly, value);
        }

        public object? MinValue
        {
            get => _minValue;
            set => SetField(ref _minValue, value);
        }

        public object? MaxValue
        {
            get => _maxValue;
            set => SetField(ref _maxValue, value);
        }

        public string? SourceCollection
        {
            get => _sourceCollection;
            set => SetField(ref _sourceCollection, value);
        }

        public string Category
        {
            get => _category;
            set => SetField(ref _category, value);
        }

        public int Order
        {
            get => _order;
            set => SetField(ref _order, value);
        }

        public string EditorType
        {
            get => _editorType;
            set => SetField(ref _editorType, value);
        }

        public string? ValidationRule
        {
            get => _validationRule;
            set => SetField(ref _validationRule, value);
        }

        /// <summary>
        /// 動的UIで使用するコントロール種別
        /// </summary>
        public SettingControlType ControlType
        {
            get => _controlType;
            set => SetField(ref _controlType, value);
        }

        /// <summary>
        /// 現在値（バインド表示用）
        /// </summary>
        public object? CurrentValue
        {
            get => _currentValue;
            set => SetField(ref _currentValue, value);
        }

        /// <summary>
        /// 現在値を値表示枠に出すかどうか
        /// </summary>
        public bool ShowCurrentValue
        {
            get => _showCurrentValue;
            set => SetField(ref _showCurrentValue, value);
        }

        /// <summary>
        /// 単位表示（NumberBox等）
        /// </summary>
        public string? Unit
        {
            get => _unit;
            set => SetField(ref _unit, value);
        }

        /// <summary>
        /// 選択肢項目（ComboBox等）
        /// </summary>
        public ObservableCollection<object>? SourceItems
        {
            get => _sourceItems;
            set => SetField(ref _sourceItems, value);
        }

        /// <summary>
        /// ファイル選択ダイアログ用フィルター
        /// </summary>
        public string? FileFilter
        {
            get => _fileFilter;
            set => SetField(ref _fileFilter, value);
        }

        public SettingDefinition() { }

        public SettingDefinition(string propertyName, string displayName, Type propertyType)
        {
            PropertyName = propertyName;
            DisplayName = displayName;
            PropertyType = propertyType;
        }

        public SettingDefinition(string propertyName, string displayName, Type propertyType, object? defaultValue)
            : this(propertyName, displayName, propertyType)
        {
            DefaultValue = defaultValue;
        }

        public override string ToString() => $"{DisplayName} ({PropertyName})";

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }
        #endregion
    }
    */
}