using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace AutoTool.Model.CommandDefinition
{
    /// <summary>
    /// 汎用CommandListItem（すべてのCommandに対応）
    /// </summary>
    public class UniversalCommandItem : INotifyPropertyChanged, IUniversalLoopItem
    {
        private bool _isEnable = true;
        private int _lineNumber;
        private bool _isRunning;
        private bool _isSelected;
        private string _itemType = string.Empty;
        private string _description = string.Empty;
        private string _comment = string.Empty;
        private int _nestLevel;
        private bool _isInLoop;
        private bool _isInIf;
        private int _progress;

        public bool IsEnable 
        { 
            get => _isEnable; 
            set => SetProperty(ref _isEnable, value); 
        }

        public int LineNumber 
        { 
            get => _lineNumber; 
            set => SetProperty(ref _lineNumber, value); 
        }

        public bool IsRunning 
        { 
            get => _isRunning; 
            set => SetProperty(ref _isRunning, value); 
        }

        public bool IsSelected 
        { 
            get => _isSelected; 
            set => SetProperty(ref _isSelected, value); 
        }

        public string ItemType 
        { 
            get => _itemType; 
            set => SetProperty(ref _itemType, value); 
        }

        public string Description 
        { 
            get => _description; 
            set => SetProperty(ref _description, value); 
        }

        public string Comment 
        { 
            get => _comment; 
            set => SetProperty(ref _comment, value); 
        }

        public int NestLevel 
        { 
            get => _nestLevel; 
            set => SetProperty(ref _nestLevel, value); 
        }

        public bool IsInLoop 
        { 
            get => _isInLoop; 
            set => SetProperty(ref _isInLoop, value); 
        }

        public bool IsInIf 
        { 
            get => _isInIf; 
            set => SetProperty(ref _isInIf, value); 
        }

        public int Progress 
        { 
            get => _progress; 
            set => SetProperty(ref _progress, value); 
        }

        /// <summary>
        /// Loop/If用のPairプロパティ
        /// </summary>
        public UniversalCommandItem? Pair { get; set; }

        /// <summary>
        /// 動的設定値
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object?> Settings { get; set; } = new();

        /// <summary>
        /// 設定定義（キャッシュ用）
        /// </summary>
        [JsonIgnore]
        public List<SettingDefinition>? SettingDefinitions { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        public UniversalCommandItem Clone()
        {
            return new UniversalCommandItem
            {
                IsEnable = IsEnable,
                LineNumber = LineNumber,
                IsRunning = IsRunning,
                IsSelected = IsSelected,
                ItemType = ItemType,
                Description = Description,
                Comment = Comment,
                NestLevel = NestLevel,
                IsInLoop = IsInLoop,
                IsInIf = IsInIf,
                Progress = Progress,
                Pair = Pair,
                Settings = new Dictionary<string, object?>(Settings),
                SettingDefinitions = SettingDefinitions?.ToList()
            };
        }

        /// <summary>
        /// 動的プロパティ取得
        /// </summary>
        public T? GetSetting<T>(string key, T? defaultValue = default)
        {
            if (Settings.TryGetValue(key, out var value))
            {
                try
                {
                    if (value is JsonElement jsonElement)
                    {
                        return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
                    }
                    if (value is T tValue) return tValue;
                    return (T?)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 動的プロパティ設定
        /// </summary>
        public void SetSetting<T>(string key, T value)
        {
            if (!Settings.ContainsKey(key) || !Equals(Settings[key], value))
            {
                Settings[key] = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"Settings[{key}]"));
            }
        }

        /// <summary>
        /// 設定定義を初期化（DirectCommandRegistryから取得）
        /// </summary>
        public void InitializeSettingDefinitions()
        {
            try
            {
                SettingDefinitions = DirectCommandRegistry.GetSettingDefinitions(ItemType);
                System.Diagnostics.Debug.WriteLine($"[UniversalCommandItem] InitializeSettingDefinitions: {ItemType} -> {SettingDefinitions?.Count ?? 0}個の設定項目");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UniversalCommandItem] InitializeSettingDefinitions Error: {ex.Message}");
                SettingDefinitions = new List<SettingDefinition>();
            }
        }
    }

    /// <summary>
    /// Loop用インターフェース実装
    /// </summary>
    public interface IUniversalLoopItem
    {
        UniversalCommandItem? Pair { get; set; }
    }
}