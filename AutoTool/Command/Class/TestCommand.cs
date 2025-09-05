using AutoTool.Command.Class;
using AutoTool.Command.Interface;
using AutoTool.Model.CommandDefinition;
using Microsoft.Extensions.Logging;
using System.Drawing;

namespace AutoTool.Command.Class
{
    [DirectCommand("Test", "テストコマンド", description: "テスト用のコマンドです")]
    public class TestCommand : BaseCommand
    {
        private readonly ILogger<TestCommand>? _logger;
        
        [SettingProperty("テストメッセージ", SettingControlType.TextBox, 
            category: "基本設定", 
            description: "実行時に表示するメッセージ", 
            defaultValue: "Hello World!")]
        public string TestMessage { get; set; } = "Hello World!";
        
        [SettingProperty("遅延時間", SettingControlType.NumberBox, 
            category: "基本設定", 
            description: "実行時の遅延時間（ミリ秒）", 
            defaultValue: 1000)]
        public int DelayMs { get; set; } = 1000;

        [SettingProperty("テスト有効", SettingControlType.CheckBox,
            category: "基本設定",
            description: "テスト機能を有効にするかどうか",
            defaultValue: true)]
        public bool IsTestEnabled { get; set; } = true;

        [SettingProperty("パスワード", SettingControlType.PasswordBox,
            category: "セキュリティ設定",
            description: "テスト用パスワード")]
        public string Password { get; set; } = string.Empty;
        
        [SettingProperty("テストファイル", SettingControlType.FilePicker, 
            description: "テスト用ファイルのパス",
            category: "ファイル設定", 
            fileFilter: "テキストファイル (*.txt)|*.txt|すべてのファイル (*.*)|*.*")]
        public string TestFilePath { get; set; } = string.Empty;

        [SettingProperty("フォルダパス", SettingControlType.FolderPicker,
            category: "ファイル設定",
            description: "テスト用フォルダのパス")]
        public string FolderPath { get; set; } = string.Empty;

        [SettingProperty("ONNXモデル", SettingControlType.OnnxPicker,
            category: "AI設定",
            description: "テスト用ONNXモデルファイル")]
        public string OnnxModelPath { get; set; } = string.Empty;

        [SettingProperty("優先度", SettingControlType.Slider,
            category: "基本設定",
            description: "処理の優先度",
            minValue: 0.0,
            maxValue: 10.0,
            defaultValue: 5.0)]
        public double Priority { get; set; } = 5.0;

        [SettingProperty("実行モード", SettingControlType.ComboBox,
            category: "基本設定",
            sourceCollection: "ExecutionModes",
            description: "実行モードを選択")]
        public string ExecutionMode { get; set; } = "Normal";

        [SettingProperty("マウス位置設定", SettingControlType.CoordinatePicker,
            description: "マウス位置を設定",
            category: "座標設定")]
        public System.Drawing.Point MousePosition { get; set; } = new(0, 0);

        [SettingProperty("色設定", SettingControlType.ColorPicker,
            category: "表示設定",
            description: "テーマカラーを設定")]
        public System.Drawing.Color ThemeColor { get; set; } = System.Drawing.Color.Blue;

        [SettingProperty("開始日", SettingControlType.DatePicker,
            category: "時間設定",
            description: "処理開始日を設定")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [SettingProperty("実行時刻", SettingControlType.TimePicker,
            category: "時間設定",
            description: "実行時刻を設定")]
        public TimeSpan ExecutionTime { get; set; } = TimeSpan.FromHours(9);

        [SettingProperty("ホットキー", SettingControlType.KeyPicker,
            category: "操作設定",
            description: "ショートカットキーを設定")]
        public System.Windows.Input.Key HotKey { get; set; } = System.Windows.Input.Key.F1;
        
        [SettingProperty("対象ウィンドウ", SettingControlType.WindowPicker, 
            category: "ウィンドウ設定",
            description: "対象とするウィンドウを選択")]
        public string WindowTitle { get; set; } = string.Empty;

        [SettingProperty("マウスボタン", SettingControlType.ComboBox,
            category: "操作設定",
            sourceCollection: "MouseButtons",
            defaultValue: System.Windows.Input.MouseButton.Left)]
        public System.Windows.Input.MouseButton MouseButton { get; set; } = System.Windows.Input.MouseButton.Left;

        // 座標の個別アクセス用プロパティ（UI表示用ではなく、内部計算用）
        public int ClickX => MousePosition.X;
        public int ClickY => MousePosition.Y;

        public TestCommand(ICommand? parent = null, UniversalCommandItem? item = null, IServiceProvider? serviceProvider = null) 
            : base(parent, null, serviceProvider)
        {
            _logger = (serviceProvider?.GetService(typeof(ILoggerFactory)) as ILoggerFactory)?.CreateLogger<TestCommand>();
            Description = "テストコマンド";

            // UniversalCommandItemから設定値を復元（DirectCommandRegistryにより自動適用される）
            if (item != null)
            {
                TestMessage = item.GetSetting("TestMessage", "Hello World!");
                DelayMs = item.GetSetting("DelayMs", 1000);
                IsTestEnabled = item.GetSetting("IsTestEnabled", true);
                Password = item.GetSetting("Password", string.Empty);
                TestFilePath = item.GetSetting("TestFilePath", string.Empty);
                FolderPath = item.GetSetting("FolderPath", string.Empty);
                OnnxModelPath = item.GetSetting("OnnxModelPath", string.Empty);
                Priority = item.GetSetting("Priority", 5.0);
                ExecutionMode = item.GetSetting("ExecutionMode", "Normal");
                MousePosition = item.GetSetting("MousePosition", new System.Drawing.Point(0, 0));
                ThemeColor = item.GetSetting("ThemeColor", System.Drawing.Color.Blue);
                StartDate = item.GetSetting("StartDate", DateTime.Today);
                ExecutionTime = item.GetSetting("ExecutionTime", TimeSpan.FromHours(9));
                HotKey = item.GetSetting("HotKey", System.Windows.Input.Key.F1);
                WindowTitle = item.GetSetting("WindowTitle", string.Empty);
                MouseButton = item.GetSetting("MouseButton", System.Windows.Input.MouseButton.Left);
            }
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation("TestCommand実行開始: {Message}, 優先度: {Priority}, モード: {Mode}", 
                    TestMessage, Priority, ExecutionMode);
                
                LogMessage($"テスト開始: {TestMessage}");
                
                if (!IsTestEnabled)
                {
                    LogMessage("テスト機能が無効のため、スキップします");
                    return true;
                }

                LogMessage($"遅延時間: {DelayMs}ms で待機中...");
                await Task.Delay(DelayMs, cancellationToken);
                
                // ファイル・フォルダ関連のテスト
                if (!string.IsNullOrEmpty(TestFilePath))
                {
                    var resolvedPath = ResolvePath(TestFilePath);
                    LogMessage($"テストファイル: {resolvedPath}");
                }

                if (!string.IsNullOrEmpty(FolderPath))
                {
                    var resolvedFolder = ResolvePath(FolderPath);
                    LogMessage($"フォルダパス: {resolvedFolder}");
                }

                if (!string.IsNullOrEmpty(OnnxModelPath))
                {
                    var resolvedModel = ResolvePath(OnnxModelPath);
                    LogMessage($"ONNXモデル: {resolvedModel}");
                }

                // 座標・操作関連のテスト
                if (MousePosition.X != 0 || MousePosition.Y != 0)
                {
                    LogMessage($"マウス位置: ({MousePosition.X}, {MousePosition.Y}) でのクリックをシミュレート（{MouseButton}ボタン）");
                }

                // 色・時間関連のテスト
                LogMessage($"テーマカラー: R={ThemeColor.R}, G={ThemeColor.G}, B={ThemeColor.B}");
                LogMessage($"開始日: {StartDate:yyyy-MM-dd}");
                LogMessage($"実行時刻: {ExecutionTime:hh\\:mm\\:ss}");

                // キー・ウィンドウ関連のテスト
                LogMessage($"ホットキー: {HotKey}");
                if (!string.IsNullOrEmpty(WindowTitle))
                {
                    LogMessage($"対象ウィンドウ: {WindowTitle}");
                }

                // セキュリティ関連のテスト
                if (!string.IsNullOrEmpty(Password))
                {
                    LogMessage("パスワードが設定されています [****]");
                }

                LogMessage($"実行モード: {ExecutionMode}, 優先度: {Priority}");
                LogMessage("TestCommand実行完了");
                _logger?.LogInformation("TestCommand実行完了: {Message}", TestMessage);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "TestCommand実行中にエラー: {Message}", ex.Message);
                LogMessage($"TestCommandエラー: {ex.Message}");
                return false;
            }
        }

        protected override void ValidateSettings()
        {
            // 設定値の検証
            if (DelayMs < 0)
            {
                throw new ArgumentException("遅延時間は0以上である必要があります");
            }

            if (!string.IsNullOrEmpty(TestFilePath))
            {
                ValidateFileExists(TestFilePath, "テストファイル");
            }

            if (Priority < 0 || Priority > 10)
            {
                throw new ArgumentException("優先度は0-10の範囲で設定してください");
            }

            if (StartDate < DateTime.Today.AddYears(-10) || StartDate > DateTime.Today.AddYears(10))
            {
                throw new ArgumentException("開始日は妥当な範囲で設定してください");
            }

            if (ExecutionTime < TimeSpan.Zero || ExecutionTime >= TimeSpan.FromDays(1))
            {
                throw new ArgumentException("実行時刻は0:00:00～23:59:59の範囲で設定してください");
            }
        }
    }
}