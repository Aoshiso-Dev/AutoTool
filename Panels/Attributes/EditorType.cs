namespace MacroPanels.Attributes;

/// <summary>
/// プロパティエディタの種類
/// </summary>
public enum EditorType
{
    /// <summary>テキスト入力</summary>
    TextBox,
    
    /// <summary>数値入力（整数）</summary>
    NumberBox,
    
    /// <summary>スライダー（範囲指定）</summary>
    Slider,
    
    /// <summary>チェックボックス</summary>
    CheckBox,
    
    /// <summary>コンボボックス（選択）</summary>
    ComboBox,
    
    /// <summary>画像ファイル選択</summary>
    ImagePicker,
    
    /// <summary>色選択</summary>
    ColorPicker,
    
    /// <summary>キー入力</summary>
    KeyPicker,
    
    /// <summary>座標選択</summary>
    PointPicker,
    
    /// <summary>ウィンドウ情報取得</summary>
    WindowInfo,
    
    /// <summary>ファイル選択</summary>
    FilePicker,
    
    /// <summary>ディレクトリ選択</summary>
    DirectoryPicker,
    
    /// <summary>マウスボタン選択</summary>
    MouseButtonPicker,
    
    /// <summary>複数行テキスト</summary>
    MultiLineTextBox
}
