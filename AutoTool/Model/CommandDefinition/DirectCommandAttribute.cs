using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoTool.Model.CommandDefinition
{
    /// <summary>
    /// DirectCommand属性によるコマンド設定定義
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DirectCommandAttribute : Attribute
    {
        public string CommandId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string Category { get; }

        public DirectCommandAttribute(string commandId, string displayName, string category = "Basic", string description = "")
        {
            CommandId = commandId;
            DisplayName = displayName;
            Category = category;
            Description = description;
        }
    }

    /// <summary>
    /// DirectSetting属性による設定項目定義
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SettingPropertyAttribute : Attribute
    {
        public string DisplayName { get; }
        public string Description { get; }
        public string? Category { get; }
        public SettingControlType ControlType { get; }
        public object? DefaultValue { get; }
        public bool IsRequired { get; }
        public bool ShowCurrentValue { get; }
        public double MinValue { get; }
        public double MaxValue { get; }
        public string? Unit { get; }
        public string? FileFilter { get; }
        public string? SourceCollection { get; }
        public string[]? ActionButtons { get; }

        public SettingPropertyAttribute(
            string displayName,
            SettingControlType controlType = SettingControlType.TextBox,
            string description = "",
            string? category = null,
            object? defaultValue = null,
            bool isRequired = false,
            bool showCurrentValue = true,
            double minValue = 0.0,
            double maxValue = 100.0,
            string? unit = null,
            string? fileFilter = null,
            string? sourceCollection = null,
            params string[]? actionButtons)
        {
            DisplayName = displayName;
            ControlType = controlType;
            Description = description;
            Category = category;
            DefaultValue = defaultValue;
            IsRequired = isRequired;
            ShowCurrentValue = showCurrentValue;
            MinValue = minValue;
            MaxValue = maxValue;
            Unit = unit;
            FileFilter = fileFilter;
            SourceCollection = sourceCollection;
            ActionButtons = actionButtons;
        }
    }
}