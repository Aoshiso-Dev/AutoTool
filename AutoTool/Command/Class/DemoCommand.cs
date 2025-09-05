using AutoTool.Command.Class;
using AutoTool.Command.Interface;
using AutoTool.Model.CommandDefinition;
using Microsoft.Extensions.Logging;

namespace AutoTool.Command.Class
{
    [DirectCommand("Demo", "デモコマンド", description: "デモ用の包括的なコマンドです")]
    public class DemoCommand : BaseCommand
    {
        private readonly ILogger<DemoCommand>? _logger;

        [SettingProperty("メッセージ", SettingControlType.TextBox,
            category: "基本設定",
            description: "表示するメッセージ",
            defaultValue: "Demo Command")]
        public string Message { get; set; } = "Demo Command";

        [SettingProperty("待機時間", SettingControlType.NumberBox,
            category: "基本設定",
            description: "待機時間（ミリ秒）",
            defaultValue: 500)]
        public int WaitTime { get; set; } = 500;

        [SettingProperty("画像ファイル", SettingControlType.FilePicker,
            description: "参照する画像ファイル",
            category: "ファイル",
            fileFilter: "画像ファイル (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|すべてのファイル (*.*)|*.*")]
        public string ImagePath { get; set; } = string.Empty;

        [SettingProperty("保存フォルダ", SettingControlType.FolderPicker,
            description: "結果を保存するフォルダ",
            category: "ファイル")]
        public string SaveFolder { get; set; } = string.Empty;

        [SettingProperty("ウィンドウタイトル", SettingControlType.WindowPicker,
            description: "対象ウィンドウのタイトル",
            category: "ウィンドウ")]
        public string WindowTitle { get; set; } = string.Empty;

        [SettingProperty("X座標", SettingControlType.NumberBox,
            description: "X座標",
            category: "座標",
            defaultValue: 100)]
        public int X { get; set; } = 100;

        [SettingProperty("Y座標", SettingControlType.NumberBox,
            description: "Y座標",
            category: "座標",
            defaultValue: 100)]
        public int Y { get; set; } = 100;

        [SettingProperty("マウス位置設定", SettingControlType.CoordinatePicker,
            description: "マウス位置を設定",
            category: "座標")]
        public System.Drawing.Point MousePosition { get; set; } = new(0, 0);

        [SettingProperty("マウスボタン", SettingControlType.ComboBox,
            category: "操作",
            description: "使用するマウスボタン",
            sourceCollection: "MouseButtons",
            defaultValue: System.Windows.Input.MouseButton.Left)]
        public System.Windows.Input.MouseButton MouseButton { get; set; } = System.Windows.Input.MouseButton.Left;

        [SettingProperty("キー", SettingControlType.ComboBox,
            category: "操作",
            description: "使用するキー",
            sourceCollection: "Keys",
            defaultValue: System.Windows.Input.Key.Enter)]
        public System.Windows.Input.Key Key { get; set; } = System.Windows.Input.Key.Enter;

        [SettingProperty("有効", SettingControlType.CheckBox,
            category: "基本設定",
            description: "このコマンドを有効にする",
            defaultValue: true)]
        public bool IsActive { get; set; } = true;

        [SettingProperty("しきい値", SettingControlType.Slider,
            category: "詳細設定",
            description: "画像認識のしきい値",
            defaultValue: 0.8)]
        public double Threshold { get; set; } = 0.8;

        [SettingProperty("パスワード", SettingControlType.PasswordBox,
            category: "認証",
            description: "接続用パスワード")]
        public string Password { get; set; } = string.Empty;

        [SettingProperty("ONNXモデル", SettingControlType.OnnxPicker,
            category: "AI",
            description: "使用するONNXモデルファイル")]
        public string OnnxModelPath { get; set; } = string.Empty;

        [SettingProperty("開始日", SettingControlType.DatePicker,
            category: "スケジュール",
            description: "処理開始日")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [SettingProperty("実行時刻", SettingControlType.TimePicker,
            category: "スケジュール",
            description: "実行する時刻")]
        public TimeSpan ExecutionTime { get; set; } = new TimeSpan(9, 0, 0);

        [SettingProperty("ホットキー", SettingControlType.KeyPicker,
            category: "操作",
            description: "設定するホットキー")]
        public System.Windows.Input.Key HotKey { get; set; } = System.Windows.Input.Key.F1;

        [SettingProperty("背景色", SettingControlType.ColorPicker,
            category: "表示",
            description: "背景に使用する色")]
        public System.Drawing.Color BackgroundColor { get; set; } = System.Drawing.Color.White;

        public DemoCommand(ICommand? parent = null, UniversalCommandItem? item = null, IServiceProvider? serviceProvider = null)
            : base(parent, null, serviceProvider)
        {
            _logger = (serviceProvider?.GetService(typeof(ILoggerFactory)) as ILoggerFactory)?.CreateLogger<DemoCommand>();
            Description = "デモコマンド";

            // UniversalCommandItemから設定値を復元
            if (item != null)
            {
                Message = item.GetSetting("Message", "Demo Command");
                WaitTime = item.GetSetting("WaitTime", 500);
                ImagePath = item.GetSetting("ImagePath", string.Empty);
                SaveFolder = item.GetSetting("SaveFolder", string.Empty);
                WindowTitle = item.GetSetting("WindowTitle", string.Empty);
                X = item.GetSetting("X", 100);
                Y = item.GetSetting("Y", 100);
                MousePosition = item.GetSetting("MousePosition", new System.Drawing.Point(0, 0));
                MouseButton = item.GetSetting("MouseButton", System.Windows.Input.MouseButton.Left);
                Key = item.GetSetting("Key", System.Windows.Input.Key.Enter);
                IsActive = item.GetSetting("IsActive", true);
                Threshold = item.GetSetting("Threshold", 0.8);
                Password = item.GetSetting("Password", string.Empty);
                OnnxModelPath = item.GetSetting("OnnxModelPath", string.Empty);
                StartDate = item.GetSetting("StartDate", DateTime.Today);
                ExecutionTime = item.GetSetting("ExecutionTime", new TimeSpan(9, 0, 0));
                HotKey = item.GetSetting("HotKey", System.Windows.Input.Key.F1);
                BackgroundColor = item.GetSetting("BackgroundColor", System.Drawing.Color.White);
            }
        }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger?.LogInformation("DemoCommand実行開始: {Message}", Message);

                LogMessage($"デモ開始: {Message}");

                if (!IsActive)
                {
                    LogMessage("コマンドが無効のため、スキップします");
                    return true;
                }

                LogMessage($"設定値チェック:");
                LogMessage($"  - 待機時間: {WaitTime}ms");
                LogMessage($"  - 画像パス: {(string.IsNullOrEmpty(ImagePath) ? "未設定" : ImagePath)}");
                LogMessage($"  - 保存フォルダ: {(string.IsNullOrEmpty(SaveFolder) ? "未設定" : SaveFolder)}");
                LogMessage($"  - ウィンドウ: {(string.IsNullOrEmpty(WindowTitle) ? "未設定" : WindowTitle)}");
                LogMessage($"  - 座標: ({X}, {Y})");
                LogMessage($"  - マウス位置: ({MousePosition.X}, {MousePosition.Y})");
                LogMessage($"  - マウスボタン: {MouseButton}");
                LogMessage($"  - キー: {Key}");
                LogMessage($"  - しきい値: {Threshold:F2}");
                LogMessage($"  - パスワード: {(string.IsNullOrEmpty(Password) ? "未設定" : "[設定済み]")}");
                LogMessage($"  - ONNXモデル: {(string.IsNullOrEmpty(OnnxModelPath) ? "未設定" : OnnxModelPath)}");
                LogMessage($"  - 開始日: {StartDate:yyyy-MM-dd}");
                LogMessage($"  - 実行時刻: {ExecutionTime:hh\\:mm\\:ss}");
                LogMessage($"  - ホットキー: {HotKey}");
                LogMessage($"  - 背景色: {BackgroundColor}");

                LogMessage($"待機中... ({WaitTime}ms)");
                await Task.Delay(WaitTime, cancellationToken);

                if (!string.IsNullOrEmpty(ImagePath))
                {
                    var resolvedPath = ResolvePath(ImagePath);
                    if (System.IO.File.Exists(resolvedPath))
                    {
                        var fileInfo = new System.IO.FileInfo(resolvedPath);
                        LogMessage($"画像ファイル確認: {resolvedPath} ({fileInfo.Length} bytes)");
                    }
                    else
                    {
                        LogMessage($"画像ファイルが見つかりません: {resolvedPath}");
                    }
                }

                if (!string.IsNullOrEmpty(SaveFolder))
                {
                    var resolvedFolder = ResolvePath(SaveFolder);
                    if (System.IO.Directory.Exists(resolvedFolder))
                    {
                        LogMessage($"保存フォルダ確認: {resolvedFolder}");
                    }
                    else
                    {
                        LogMessage($"保存フォルダが見つかりません: {resolvedFolder}");
                    }
                }

                LogMessage("DemoCommand実行完了");
                _logger?.LogInformation("DemoCommand実行完了: {Message}", Message);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "DemoCommand実行中にエラー: {Message}", ex.Message);
                LogMessage($"DemoCommandエラー: {ex.Message}");
                return false;
            }
        }

        protected override void ValidateSettings()
        {
            if (WaitTime < 0)
            {
                throw new ArgumentException("待機時間は0以上である必要があります");
            }

            if (!string.IsNullOrEmpty(ImagePath))
            {
                ValidateFileExists(ImagePath, "画像ファイル");
            }

            if (!string.IsNullOrEmpty(OnnxModelPath))
            {
                ValidateFileExists(OnnxModelPath, "ONNXモデルファイル");
            }

            if (Threshold < 0.0 || Threshold > 1.0)
            {
                throw new ArgumentException("しきい値は0.0～1.0の範囲で設定してください");
            }

            if (StartDate < DateTime.Today.AddYears(-10) || StartDate > DateTime.Today.AddYears(10))
            {
                throw new ArgumentException("開始日は妥当な範囲で設定してください");
            }

            if (ExecutionTime < TimeSpan.Zero || ExecutionTime >= TimeSpan.FromDays(1))
            {
                throw new ArgumentException("実行時刻は0:00:00?23:59:59の範囲で設定してください");
            }
        }
    }
}