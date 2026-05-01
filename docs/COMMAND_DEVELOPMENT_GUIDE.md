# コマンド開発ガイド（現行実装準拠）

このガイドは、`AutoTool` の現行コードに合わせて、新しいコマンドを追加する手順をまとめたものです。

## 概要

現在のコマンド追加は、`AutoTool.Automation.Runtime/Model/Panels/List/Class/CommandListItem*.cs` に `CommandListItem` 派生クラスを追加する方式です。  
`[CommandDefinition]` で定義したクラスは、以下に自動反映されます。

- コマンド一覧表示（カテゴリ・表示名・表示順）
- `ICommandRegistry` からの生成
- `ICommandListItem` のポリモーフィックシリアライズ

## 最重要ルール

1. **`[CommandDefinition]` は必須**  
   これがないと一覧にもシリアライズにも乗りません。

2. **`ExecuteAsync` をオーバーライドする場合**  
   `SimpleCommandBinding` は不要です。  
   `ReflectionCommandRegistry` が `ExecuteAsync` オーバーライドを検出し、`SimpleCommand` 経由で実行します。

3. **`ExecuteAsync` をオーバーライドしない場合**  
   `[SimpleCommandBinding]` が必要です。  
   典型例: `IfEndItem` / `LoopEndItem` / `LoopBreakItem`

4. **`ExecuteAsync` のシグネチャは `ValueTask<bool>`**

```csharp
public override async ValueTask<bool> ExecuteAsync(
    ICommandExecutionContext context,
    CancellationToken cancellationToken)
```

5. **引数型は現行の入力モデルを使用**

- マウス: `CommandMouseButton`
- キー: `CommandKey`

## 追加手順（推奨）

1. `CommandListItem*.cs` に `partial class` を追加する
2. `CommandTypeNames` の既存定数を使う（新規 type 名を作る場合は定数追加）
3. `CommandProperty` 付きプロパティを定義する
4. `Description` を実装する
5. `Clone()` とコピーコンストラクタを実装する
6. 必要なら `ExecuteAsync` を実装する
7. ビルドして一覧・実行・保存/読込を確認する

## 実装テンプレート（`ExecuteAsync` あり）

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using AutoTool.Commands.Commands;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Model.Input;
using AutoTool.Automation.Contracts.Lists;
using AutoTool.Automation.Runtime.Attributes;
using CommandDef = AutoTool.Automation.Runtime.Definitions;

namespace AutoTool.Automation.Runtime.Lists;

[CommandDef.CommandDefinition(
    CommandDef.CommandTypeNames.ClickImage,
    typeof(SimpleCommand),
    typeof(IClickImageCommandSettings),
    CommandDef.CommandCategory.Click,
    displayPriority: 1,
    displaySubPriority: 99,
    displayNameJa: "サンプル",
    displayNameEn: "Sample")]
public partial class SampleItem : CommandListItem, IClickImageItem, IClickImageCommandSettings
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("検索画像", EditorType.ImagePicker, Group = "画像設定", Order = 1)]
    private string _imagePath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("一致しきい値", EditorType.Slider, Group = "画像設定", Order = 2, Min = 0.01, Max = 1.0, Step = 0.01)]
    private double _threshold = 0.8;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("マウスボタン", EditorType.MouseButtonPicker, Group = "クリック設定", Order = 1)]
    private CommandMouseButton _button = CommandMouseButton.Left;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("押下維持時間", EditorType.NumberBox, Group = "クリック設定", Order = 2, Min = 0, Unit = "ミリ秒")]
    private int _holdDurationMs = 20;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("注入方式", EditorType.ComboBox, Group = "クリック設定", Order = 3, Options = "MouseEvent,SendInput")]
    private string _clickInjectionMode = "MouseEvent";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("移動シミュレート", EditorType.CheckBox, Group = "クリック設定", Order = 4)]
    private bool _simulateMouseMove = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("クリック後に元の位置へ戻す", EditorType.CheckBox, Group = "クリック設定", Order = 5)]
    private bool _restoreCursorPositionAfterClick = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("クリック後にウィンドウ順を戻す", EditorType.CheckBox, Group = "クリック設定", Order = 6)]
    private bool _restoreWindowZOrderAfterClick = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("ウィンドウタイトル", EditorType.WindowInfo, Group = "対象ウィンドウ", Order = 1)]
    private string _windowTitle = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("ウィンドウクラス名", EditorType.TextBox, Group = "対象ウィンドウ", Order = 2)]
    private string _windowClassName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("タイムアウト", EditorType.Duration, Group = "タイミング", Order = 1, Min = 0)]
    private int _timeout = 5000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    [property: CommandProperty("検索間隔", EditorType.Duration, Group = "タイミング", Order = 2, Min = 0)]
    private int _interval = 500;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    private CommandColor? _searchColor = null;

    new public string Description =>
        $"対象:{(string.IsNullOrEmpty(WindowTitle) && string.IsNullOrEmpty(WindowClassName) ? "グローバル" : $"{WindowTitle}[{WindowClassName}]")} / 画像:{System.IO.Path.GetFileName(ImagePath)} / 閾値:{Threshold}";

    public SampleItem() { }

    public SampleItem(SampleItem? item = null) : base(item)
    {
        if (item is null)
        {
            return;
        }

        ImagePath = item.ImagePath;
        Threshold = item.Threshold;
        Button = item.Button;
        WindowTitle = item.WindowTitle;
        WindowClassName = item.WindowClassName;
        Timeout = item.Timeout;
        Interval = item.Interval;
        SearchColor = item.SearchColor;
    }

    public new ICommandListItem Clone() => new SampleItem(this);

    public override async ValueTask<bool> ExecuteAsync(ICommandExecutionContext context, CancellationToken cancellationToken)
    {
        var absolutePath = context.ToAbsolutePath(ImagePath);
        var started = System.Diagnostics.Stopwatch.StartNew();

        while (started.ElapsedMilliseconds < Timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var point = await context.SearchImageAsync(
                absolutePath,
                Threshold,
                SearchColor,
                WindowTitle,
                WindowClassName,
                cancellationToken).ConfigureAwait(false);

            if (point is not null)
            {
                await context.ClickAsync(
                    point.Value.X,
                    point.Value.Y,
                    Button,
                    WindowTitle,
                    WindowClassName,
                    HoldDurationMs,
                    ClickInjectionMode,
                    SimulateMouseMove).ConfigureAwait(false);

                context.Log($"クリック成功: ({point.Value.X}, {point.Value.Y})");
                return true;
            }

            await Task.Delay(Interval, cancellationToken).ConfigureAwait(false);
        }

        context.Log("タイムアウト: 画像が見つかりませんでした。");
        return false;
    }
}
```

## `SimpleCommandBinding` が必要なケース

`ExecuteAsync` を実装せず、既存コマンドクラスへ委譲する場合に使います。

```csharp
[CommandDef.SimpleCommandBinding(typeof(LoopEndCommand), typeof(ICommandSettings))]
[CommandDef.CommandDefinition(
    CommandDef.CommandTypeNames.LoopEnd,
    typeof(LoopEndCommand),
    typeof(ICommandSettings),
    CommandDef.CommandCategory.Control,
    isEndCommand: true)]
public partial class LoopEndItem : CommandListItem, ILoopEndItem, ICommandSettings
{
    // ExecuteAsync を override しない
}
```

## `CommandProperty` 属性

`CommandPropertyAttribute` の主なパラメータ:

- `displayName`（コンストラクタ必須）
- `editorType`（コンストラクタ必須）
- `Group`
- `Order`
- `Description`
- `Min`
- `Max`
- `Step`
- `Unit`
- `Options`
- `FileFilter`

プラグイン由来の `FilePicker` でも、`properties[].fileFilter` に `JSON Files (*.json)|*.json|All Files (*.*)|*.*` のような WPF `OpenFileDialog.Filter` 形式の文字列を指定できます。

## `EditorType` 一覧

- `TextBox`
- `NumberBox`
- `Slider`
- `CheckBox`
- `ComboBox`
- `ImagePicker`
- `ColorPicker`
- `KeyPicker`
- `PointPicker`
- `WindowInfo`
- `FilePicker`
- `DirectoryPicker`
- `MouseButtonPicker`
- `MultiLineTextBox`

## `ICommandExecutionContext`（抜粋）

```csharp
DateTimeOffset GetLocalNow();
void ReportProgress(int progress);
void Log(string message);
string? GetVariable(string name);
void SetVariable(string name, string value);
string ToAbsolutePath(string relativePath);
Task ClickAsync(int x, int y, CommandMouseButton button, string? windowTitle = null, string? windowClassName = null, int holdDurationMs = 20, string clickInjectionMode = "MouseEvent", bool simulateMouseMove = false);
Task SendHotkeyAsync(CommandKey key, bool ctrl, bool alt, bool shift, string? windowTitle = null, string? windowClassName = null);
Task ExecuteProgramAsync(string programPath, string? arguments, string? workingDirectory, bool waitForExit, CancellationToken cancellationToken);
Task TakeScreenshotAsync(string filePath, string? windowTitle, string? windowClassName, CancellationToken cancellationToken);
Task<MatchPoint?> SearchImageAsync(string imagePath, double threshold, CommandColor? searchColor, string? windowTitle, string? windowClassName, CancellationToken cancellationToken);
void InitializeAIModel(string modelPath, int inputSize = 640, bool useGpu = true);
IReadOnlyList<DetectionResult> DetectAI(string? windowTitle, float confThreshold, float iouThreshold);
Task<OcrExtractionResult> ExtractTextAsync(OcrRequest request, CancellationToken cancellationToken);
```

## コマンドカテゴリ

```csharp
CommandDef.CommandCategory.Click      // クリック操作
CommandDef.CommandCategory.Input      // キー入力
CommandDef.CommandCategory.Wait       // 待機
CommandDef.CommandCategory.Condition  // 条件分岐
CommandDef.CommandCategory.Control    // 繰り返し・リトライ
CommandDef.CommandCategory.Variable   // 変数操作
CommandDef.CommandCategory.System     // システム操作
```

カテゴリは「AI」「OCR」「画像検索」などの内部技術ではなく、利用者から見た実際の操作で選びます。
例: `画像クリック(AI検出)` は `Click`、`AI画像存在判定` は `Condition`、`AI変数設定` は `Variable` に分類します。

## チェックリスト

1. `CommandDefinition` を付けたか
2. 引数なしコンストラクタがあるか
3. `Clone()` で新インスタンスを返しているか
4. `Description` が編集内容を反映するか
5. 実行処理で `CancellationToken` を考慮しているか
6. 長時間処理で `ReportProgress` / `Log` を使っているか
7. ファイルパスは `ToAbsolutePath` を使っているか
8. `dotnet build` 後、UI 一覧・実行・保存/読込が通るか

## トラブルシューティング

### コマンドが一覧に出ない

- `[CommandDefinition]` の付け忘れ
- `TypeName` 重複
- ビルド未反映

### 実行時に「バインディング属性がありません」と出る

- `ExecuteAsync` を override していないのに `SimpleCommandBinding` がない

### UI エディタが出ない

- `[property: CommandProperty(...)]` の指定漏れ
- `EditorType` と型の組み合わせが不適切

### 保存/読込で失敗する

- `TypeName` を既存保存データと互換性のない形で変更した
