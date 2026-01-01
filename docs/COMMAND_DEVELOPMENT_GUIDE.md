# コマンド開発ガイド

このガイドでは、MacroPanelsに新しいコマンドを追加する方法を説明します。

## 概要

新しいコマンド追加システムでは、**1ファイルで完結**する形式を採用しています。
`CommandListItem.cs`に新しいクラスを追加するだけで、UIとロジックが自動的に統合されます。

## 基本構造

```csharp
[CommandDef.SimpleCommandBinding(typeof(LegacyCommand), typeof(ISettings))]  // 互換性用（オプション）
[CommandDef.CommandDefinition("CommandType", typeof(Command), typeof(ISettings), CommandDef.CommandCategory.Action)]
public partial class NewCommandItem : CommandListItem, INewCommandItem, INewCommandSettings
{
    // 1. プロパティ定義（UIエディタ自動生成）
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("表示名", EditorType.NumberBox, Group = "基本設定", Order = 1,
                     Description = "説明文", Unit = "単位", Min = 0, Max = 100)]
    private int _value = 50;

    // 2. Description（リスト表示用）
    new public string Description => $"値: {Value}";

    // 3. コンストラクタ
    public NewCommandItem() { }
    public NewCommandItem(NewCommandItem? item = null) : base(item)
    {
        if (item != null)
        {
            Value = item.Value;
        }
    }

    // 4. Clone
    public new ICommandListItem Clone() => new NewCommandItem(this);

    // 5. 実行ロジック
    public override async Task<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
    {
        // ここに処理を書く
        context.Log("実行完了！");
        return true;
    }
}
```

## CommandProperty属性

プロパティにUIエディタを自動生成します。

| パラメータ | 説明 | 例 |
|-----------|------|-----|
| 表示名 | UIに表示される名前 | "待機時間" |
| EditorType | エディタの種類 | EditorType.NumberBox |
| Group | グループ化 | "基本設定" |
| Order | グループ内の表示順 | 1 |
| Description | ツールチップ説明 | "待機する時間（ミリ秒）" |
| Unit | 単位表示 | "ミリ秒" |
| Min/Max | 数値の範囲 | Min = 0, Max = 10000 |
| Step | スライダーのステップ | 0.01 |
| Options | ComboBoxの選択肢 | "Option1,Option2,Option3" |
| FileFilter | ファイル選択フィルター | "画像|*.png;*.jpg" |

## EditorType一覧

| EditorType | 説明 | 対応する型 |
|------------|------|-----------|
| TextBox | 1行テキスト入力 | string |
| MultiLineTextBox | 複数行テキスト | string |
| NumberBox | 数値入力 | int, double |
| Slider | スライダー | double |
| CheckBox | チェックボックス | bool |
| ComboBox | ドロップダウン | string |
| ColorPicker | 色選択 | Color? |
| FilePicker | ファイル選択 | string |
| DirectoryPicker | フォルダ選択 | string |
| ImagePicker | 画像選択 | string |
| PointPicker | 座標選択 | int (X, Y) |
| KeyPicker | キー選択 | Key |
| MouseButtonPicker | マウスボタン選択 | MouseButton |
| WindowInfo | ウィンドウ選択 | string |

## ICommandExecutionContext メソッド一覧

実行時に使用可能なメソッドです。

### 基本操作

```csharp
// 進捗報告（0-100）
context.ReportProgress(50);

// ログ出力
context.Log("メッセージ");

// 変数操作
context.SetVariable("name", "value");
string? val = context.GetVariable("name");

// パス変換
string abs = context.ToAbsolutePath("relative/path");
```

### 入力操作

```csharp
// マウスクリック
await context.ClickAsync(x, y, MouseButton.Left, windowTitle, windowClassName);

// キー送信
await context.SendHotkeyAsync(Key.A, ctrl: true, alt: false, shift: false, windowTitle, windowClassName);
```

### システム操作

```csharp
// プログラム実行
await context.ExecuteProgramAsync(path, args, workDir, waitForExit, cancellationToken);

// スクリーンショット
await context.TakeScreenshotAsync(filePath, windowTitle, windowClassName, cancellationToken);
```

### 画像検索

```csharp
// 画像検索（見つかった場合は座標を返す）
var point = await context.SearchImageAsync(imagePath, threshold, searchColor, windowTitle, windowClassName, cancellationToken);
if (point != null)
{
    // 画像が見つかった
    int x = point.Value.X;
    int y = point.Value.Y;
}
```

### AI検出

```csharp
// モデル初期化
context.InitializeAIModel(modelPath, inputSize: 640, useGpu: true);

// 物体検出
var detections = context.DetectAI(windowTitle, confThreshold, iouThreshold);
foreach (var det in detections)
{
    int classId = det.ClassId;
    float score = det.Score;
    Rectangle rect = det.Rect;
}
```

## コマンドカテゴリ

```csharp
CommandDef.CommandCategory.Action    // 基本操作（クリック、待機など）
CommandDef.CommandCategory.Control   // 制御（ループ、条件分岐）
CommandDef.CommandCategory.Variable  // 変数操作
CommandDef.CommandCategory.System    // システム（実行、スクリーンショット）
CommandDef.CommandCategory.AI        // AI関連
```

## 完全な実装例

### 例1: シンプルな待機コマンド

```csharp
[CommandDef.SimpleCommandBinding(typeof(WaitCommand), typeof(IWaitCommandSettings))]
[CommandDef.CommandDefinition("Wait", typeof(WaitCommand), typeof(IWaitCommandSettings), CommandDef.CommandCategory.Action)]
public partial class WaitItem : CommandListItem, IWaitItem, IWaitCommandSettings
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("待機時間", EditorType.NumberBox, Group = "基本設定", Order = 1,
                     Description = "指定した時間待機します", Unit = "ミリ秒", Min = 0)]
    private int _wait = 5000;

    new public string Description => $"待機時間:{Wait}ms";
    
    public WaitItem() { }
    public WaitItem(WaitItem? item = null) : base(item)
    {
        if (item != null) Wait = item.Wait;
    }
    public new ICommandListItem Clone() => new WaitItem(this);
    
    public override async Task<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        while (stopwatch.ElapsedMilliseconds < Wait)
        {
            if (cancellationToken.IsCancellationRequested) return false;
            
            var progress = Wait > 0 ? (int)((stopwatch.ElapsedMilliseconds * 100) / Wait) : 100;
            context.ReportProgress(Math.Clamp(progress, 0, 100));
            
            await Task.Delay(100, cancellationToken);
        }
        
        context.Log("待機完了");
        return true;
    }
}
```

### 例2: 画像クリックコマンド

```csharp
[CommandDef.SimpleCommandBinding(typeof(ClickImageCommand), typeof(IClickImageCommandSettings))]
[CommandDef.CommandDefinition("Click_Image", typeof(ClickImageCommand), typeof(IClickImageCommandSettings), CommandDef.CommandCategory.Action)]
public partial class ClickImageItem : CommandListItem, IClickImageItem, IClickImageCommandSettings
{
    [ObservableProperty]
    [property: CommandProperty("検索画像", EditorType.ImagePicker, Group = "画像設定", Order = 1)]
    private string _imagePath = string.Empty;
    
    [ObservableProperty]
    [property: CommandProperty("一致しきい値", EditorType.Slider, Group = "画像設定", Order = 2, Min = 0.01, Max = 1.0, Step = 0.01)]
    private double _threshold = 0.8;
    
    // ... 他のプロパティ ...
    
    public override async Task<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
    {
        var absolutePath = context.ToAbsolutePath(ImagePath);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < Timeout)
        {
            var point = await context.SearchImageAsync(absolutePath, Threshold, SearchColor, WindowTitle, WindowClassName, cancellationToken);

            if (point != null)
            {
                await context.ClickAsync(point.Value.X, point.Value.Y, Button, WindowTitle, WindowClassName);
                context.Log($"画像をクリックしました。({point.Value.X}, {point.Value.Y})");
                return true;
            }

            if (cancellationToken.IsCancellationRequested) return false;
            await Task.Delay(Interval, cancellationToken);
        }

        context.Log("画像が見つかりませんでした。");
        return false;
    }
}
```

## 注意事項

1. **キャンセル対応**: `cancellationToken.IsCancellationRequested`を確認してください
2. **進捗報告**: 長時間の処理では`ReportProgress`を呼び出してください
3. **ログ出力**: 重要な状態変化は`Log`で報告してください
4. **例外処理**: 必要に応じてtry-catchで例外を処理してください
5. **相対パス**: ファイルパスは`ToAbsolutePath`で絶対パスに変換してください

## トラブルシューティング

### コマンドがリストに表示されない
- `CommandDefinition`属性が正しく設定されているか確認
- `CommandRegistry.Initialize()`が呼ばれているか確認

### ExecuteAsyncが呼ばれない
- `override`キーワードがあるか確認
- `SimpleCommandBinding`属性が付いているか確認
- ビルドが成功しているか確認

### UIエディタが表示されない
- `CommandProperty`属性が正しく設定されているか確認
- `EditorType`が対応する型と一致しているか確認
