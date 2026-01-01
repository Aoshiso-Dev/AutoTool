namespace MacroPanels.Attributes;

/// <summary>
/// コマンドプロパティにUI情報を付与する属性
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class CommandPropertyAttribute : Attribute
{
    /// <summary>表示名</summary>
    public string DisplayName { get; }
    
    /// <summary>エディタの種類</summary>
    public EditorType EditorType { get; }
    
    /// <summary>グループ名（同じグループは同じカードに表示）</summary>
    public string Group { get; set; } = "基本設定";
    
    /// <summary>表示順序（グループ内での順序）</summary>
    public int Order { get; set; } = 0;
    
    /// <summary>説明文</summary>
    public string? Description { get; set; }
    
    /// <summary>最小値（NumberBox, Slider用）</summary>
    public double Min { get; set; } = 0;
    
    /// <summary>最大値（NumberBox, Slider用）</summary>
    public double Max { get; set; } = double.MaxValue;
    
    /// <summary>ステップ値（Slider用）</summary>
    public double Step { get; set; } = 1;
    
    /// <summary>単位（表示用）</summary>
    public string? Unit { get; set; }
    
    /// <summary>コンボボックスの選択肢（カンマ区切り）</summary>
    public string? Options { get; set; }
    
    /// <summary>ファイルフィルター（FilePicker用）</summary>
    public string? FileFilter { get; set; }
    
    /// <summary>
    /// コマンドプロパティ属性を作成
    /// </summary>
    /// <param name="displayName">表示名</param>
    /// <param name="editorType">エディタの種類</param>
    public CommandPropertyAttribute(string displayName, EditorType editorType)
    {
        DisplayName = displayName;
        EditorType = editorType;
    }
}
