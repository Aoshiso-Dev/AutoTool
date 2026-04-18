namespace AutoTool.Automation.Runtime.Attributes;

/// <summary>
/// プロパティ編集に使用する UI エディタ種別。
/// </summary>
public enum EditorType
{
    /// <summary>単一行テキスト入力</summary>
    TextBox,

    /// <summary>数値入力</summary>
    NumberBox,

    /// <summary>スライダー入力</summary>
    Slider,

    /// <summary>チェックボックス</summary>
    CheckBox,

    /// <summary>コンボボックス</summary>
    ComboBox,

    /// <summary>画像ファイル選択</summary>
    ImagePicker,

    /// <summary>色選択</summary>
    ColorPicker,

    /// <summary>キー選択</summary>
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

    /// <summary>複数行テキスト入力</summary>
    MultiLineTextBox
}
