using System.Windows;
using System.Windows.Controls;
using MacroPanels.Attributes;

namespace MacroPanels.View.Editors;

/// <summary>
/// PropertyMetadata‚ÌEditorType‚ÉŠî‚Ã‚¢‚ÄDataTemplate‚ð‘I‘ð
/// </summary>
public class EditorTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TextBoxTemplate { get; set; }
    public DataTemplate? NumberBoxTemplate { get; set; }
    public DataTemplate? SliderTemplate { get; set; }
    public DataTemplate? CheckBoxTemplate { get; set; }
    public DataTemplate? ComboBoxTemplate { get; set; }
    public DataTemplate? ImagePickerTemplate { get; set; }
    public DataTemplate? ColorPickerTemplate { get; set; }
    public DataTemplate? KeyPickerTemplate { get; set; }
    public DataTemplate? PointPickerTemplate { get; set; }
    public DataTemplate? WindowInfoTemplate { get; set; }
    public DataTemplate? FilePickerTemplate { get; set; }
    public DataTemplate? DirectoryPickerTemplate { get; set; }
    public DataTemplate? MouseButtonPickerTemplate { get; set; }
    public DataTemplate? MultiLineTextBoxTemplate { get; set; }
    public DataTemplate? DefaultTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is not Attributes.PropertyMetadata metadata)
            return DefaultTemplate ?? base.SelectTemplate(item, container);

        return metadata.EditorType switch
        {
            EditorType.TextBox => TextBoxTemplate ?? DefaultTemplate,
            EditorType.NumberBox => NumberBoxTemplate ?? DefaultTemplate,
            EditorType.Slider => SliderTemplate ?? DefaultTemplate,
            EditorType.CheckBox => CheckBoxTemplate ?? DefaultTemplate,
            EditorType.ComboBox => ComboBoxTemplate ?? DefaultTemplate,
            EditorType.ImagePicker => ImagePickerTemplate ?? DefaultTemplate,
            EditorType.ColorPicker => ColorPickerTemplate ?? DefaultTemplate,
            EditorType.KeyPicker => KeyPickerTemplate ?? DefaultTemplate,
            EditorType.PointPicker => PointPickerTemplate ?? DefaultTemplate,
            EditorType.WindowInfo => WindowInfoTemplate ?? DefaultTemplate,
            EditorType.FilePicker => FilePickerTemplate ?? DefaultTemplate,
            EditorType.DirectoryPicker => DirectoryPickerTemplate ?? DefaultTemplate,
            EditorType.MouseButtonPicker => MouseButtonPickerTemplate ?? DefaultTemplate,
            EditorType.MultiLineTextBox => MultiLineTextBoxTemplate ?? DefaultTemplate,
            _ => DefaultTemplate ?? base.SelectTemplate(item, container)
        };
    }
}
