using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Linq;
using AutoTool.Services.Plugin;
using MacroPanels.Command.Interface;
using MacroPanels.Plugin;
using OpenCVHelper;
using KeyHelper;
using MouseHelper;
using System.Windows.Input;

namespace AutoTool.SamplePlugins
{
    /// <summary>
    /// サンプルプラグインコマンドの設定
    /// </summary>
    public interface ISampleCommandSettings : ICommandSettings
    {
        string Message { get; set; }
        int DelayMs { get; set; }
    }

    /// <summary>
    /// サンプル設定の実装
    /// </summary>
    public class SampleCommandSettings : ISampleCommandSettings
    {
        public string Message { get; set; } = "Hello from Plugin!";
        public int DelayMs { get; set; } = 1000;
    }

    /// <summary>
    /// サンプルプラグインコマンド
    /// </summary>
    public class SamplePluginCommand : PluginCommandBase
    {
        public new ISampleCommandSettings? Settings => base.Settings as ISampleCommandSettings;

        public SamplePluginCommand(MacroPanels.Plugin.IPluginInfo pluginInfo, MacroPanels.Plugin.IPluginCommandInfo commandInfo, 
            MacroPanels.Command.Interface.ICommand? parent = null, object? settings = null) 
            : base(pluginInfo, commandInfo, parent, settings)
        {
        }

        protected override async Task<bool> DoExecutePluginAsync(CancellationToken cancellationToken)
        {
            var settings = Settings ?? new SampleCommandSettings();
            
            LogMessage($"プラグインコマンド開始: {settings.Message}");
            
            // 指定した時間だけ待機
            var startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalMilliseconds < settings.DelayMs)
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;

                // 進捗報告
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                ReportProgress(elapsed, settings.DelayMs);
                
                await Task.Delay(50, cancellationToken);
            }
            
            LogMessage($"プラグインコマンド完了: {settings.Message}");
            return true;
        }

        protected override void ValidatePluginSettings()
        {
            var settings = Settings;
            if (settings != null)
            {
                if (settings.DelayMs < 0)
                    throw new ArgumentException("DelayMsは0以上である必要があります");
                    
                if (string.IsNullOrEmpty(settings.Message))
                    throw new ArgumentException("Messageは必須です");
            }
        }
    }

    /// <summary>
    /// ファイル操作プラグインコマンドの設定
    /// </summary>
    public interface IFileOperationSettings : ICommandSettings
    {
        string SourcePath { get; set; }
        string DestinationPath { get; set; }
        string Operation { get; set; } // "Copy", "Move", "Delete"
    }

    /// <summary>
    /// ファイル操作設定の実装
    /// </summary>
    public class FileOperationSettings : IFileOperationSettings
    {
        public string SourcePath { get; set; } = "";
        public string DestinationPath { get; set; } = "";
        public string Operation { get; set; } = "Copy";
    }

    /// <summary>
    /// ファイル操作プラグインコマンド
    /// </summary>
    public class FileOperationCommand : PluginCommandBase
    {
        public new IFileOperationSettings? Settings => base.Settings as IFileOperationSettings;

        public FileOperationCommand(MacroPanels.Plugin.IPluginInfo pluginInfo, MacroPanels.Plugin.IPluginCommandInfo commandInfo, 
            MacroPanels.Command.Interface.ICommand? parent = null, object? settings = null) 
            : base(pluginInfo, commandInfo, parent, settings)
        {
        }

        protected override async Task<bool> DoExecutePluginAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            try
            {
                LogMessage($"ファイル操作開始: {settings.Operation}");

                await Task.Run(() =>
                {
                    switch (settings.Operation.ToLower())
                    {
                        case "copy":
                            File.Copy(settings.SourcePath, settings.DestinationPath, true);
                            LogMessage($"ファイルをコピーしました: {settings.SourcePath} -> {settings.DestinationPath}");
                            break;

                        case "move":
                            if (File.Exists(settings.DestinationPath))
                                File.Delete(settings.DestinationPath);
                            File.Move(settings.SourcePath, settings.DestinationPath);
                            LogMessage($"ファイルを移動しました: {settings.SourcePath} -> {settings.DestinationPath}");
                            break;

                        case "delete":
                            File.Delete(settings.SourcePath);
                            LogMessage($"ファイルを削除しました: {settings.SourcePath}");
                            break;

                        default:
                            throw new ArgumentException($"未対応の操作: {settings.Operation}");
                    }
                }, cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"? ファイル操作エラー: {ex.Message}");
                return false;
            }
        }

        protected override void ValidateFiles()
        {
            var settings = Settings;
            if (settings != null)
            {
                ValidateFileExists(settings.SourcePath, "操作対象ファイル");
                
                if (settings.Operation.ToLower() != "delete" && !string.IsNullOrEmpty(settings.DestinationPath))
                {
                    var destDir = Path.GetDirectoryName(settings.DestinationPath);
                    if (!string.IsNullOrEmpty(destDir))
                    {
                        ValidateDirectoryExists(destDir, "出力先ディレクトリ");
                    }
                }
            }
        }

        protected override void ValidatePluginSettings()
        {
            var settings = Settings;
            if (settings != null)
            {
                if (string.IsNullOrEmpty(settings.SourcePath))
                    throw new ArgumentException("SourcePathは必須です");

                var validOperations = new[] { "copy", "move", "delete" };
                if (!Array.Exists(validOperations, op => op.Equals(settings.Operation, StringComparison.OrdinalIgnoreCase)))
                    throw new ArgumentException($"無効な操作: {settings.Operation}");

                if (settings.Operation.ToLower() != "delete" && string.IsNullOrEmpty(settings.DestinationPath))
                    throw new ArgumentException("Copy/Move操作にはDestinationPathが必要です");
            }
        }
    }

    #region 既存コマンドのプラグイン版実装

    /// <summary>
    /// 画像待機プラグインコマンド
    /// </summary>
    public class WaitImagePluginCommand : PluginCommandBase
    {
        public new IWaitImageCommandSettings? Settings => base.Settings as IWaitImageCommandSettings;

        public WaitImagePluginCommand(MacroPanels.Plugin.IPluginInfo pluginInfo, MacroPanels.Plugin.IPluginCommandInfo commandInfo, 
            MacroPanels.Command.Interface.ICommand? parent = null, object? settings = null) 
            : base(pluginInfo, commandInfo, parent, settings)
        {
        }

        protected override void ValidatePluginSettings()
        {
            var settings = Settings;
            if (settings != null)
            {
                if (string.IsNullOrEmpty(settings.ImagePath))
                    throw new ArgumentException("ImagePathは必須です");
                
                ValidateFileExists(settings.ImagePath, "画像ファイル");
                
                if (settings.Timeout <= 0)
                    throw new ArgumentException("Timeoutは0より大きい値である必要があります");
                    
                if (settings.Interval <= 0)
                    throw new ArgumentException("Intervalは0より大きい値である必要があります");
            }
        }

        protected override async Task<bool> DoExecutePluginAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            LogMessage($"画像待機開始: {settings.ImagePath}");
            
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < settings.Timeout)
            {
                var point = await ImageSearchHelper.SearchImage(
                    settings.ImagePath, cancellationToken, settings.Threshold, 
                    settings.SearchColor, settings.WindowTitle, settings.WindowClassName);

                if (point != null)
                {
                    LogMessage($"画像が見つかりました。({point.Value.X}, {point.Value.Y})");
                    return true;
                }

                if (cancellationToken.IsCancellationRequested) return false;

                ReportProgress(stopwatch.ElapsedMilliseconds, settings.Timeout);
                await Task.Delay(settings.Interval, cancellationToken);
            }

            LogMessage("画像が見つかりませんでした。");
            return false;
        }
    }

    /// <summary>
    /// 画像クリックプラグインコマンド
    /// </summary>
    public class ClickImagePluginCommand : PluginCommandBase
    {
        public new IClickImageCommandSettings? Settings => base.Settings as IClickImageCommandSettings;

        public ClickImagePluginCommand(MacroPanels.Plugin.IPluginInfo pluginInfo, MacroPanels.Plugin.IPluginCommandInfo commandInfo, 
            MacroPanels.Command.Interface.ICommand? parent = null, object? settings = null) 
            : base(pluginInfo, commandInfo, parent, settings)
        {
        }

        protected override void ValidatePluginSettings()
        {
            var settings = Settings;
            if (settings != null)
            {
                if (string.IsNullOrEmpty(settings.ImagePath))
                    throw new ArgumentException("ImagePathは必須です");
                
                ValidateFileExists(settings.ImagePath, "画像ファイル");
                
                if (settings.Timeout <= 0)
                    throw new ArgumentException("Timeoutは0より大きい値である必要があります");
                    
                if (settings.Interval <= 0)
                    throw new ArgumentException("Intervalは0より大きい値である必要があります");
            }
        }

        protected override async Task<bool> DoExecutePluginAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            LogMessage($"画像クリック開始: {settings.ImagePath}");
            
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < settings.Timeout)
            {
                var point = await ImageSearchHelper.SearchImage(
                    settings.ImagePath, cancellationToken, settings.Threshold,
                    settings.SearchColor, settings.WindowTitle, settings.WindowClassName);

                if (point != null)
                {
                    await ExecuteMouseClick(point.Value.X, point.Value.Y, settings.Button, 
                        settings.WindowTitle, settings.WindowClassName);
                    
                    LogMessage($"画像をクリックしました。({point.Value.X}, {point.Value.Y})");
                    return true;
                }

                if (cancellationToken.IsCancellationRequested) return false;

                ReportProgress(stopwatch.ElapsedMilliseconds, settings.Timeout);
                await Task.Delay(settings.Interval, cancellationToken);
            }

            LogMessage("画像が見つかりませんでした。");
            return false;
        }

        private static async Task ExecuteMouseClick(int x, int y, MouseButton button, 
            string windowTitle, string windowClassName)
        {
            switch (button)
            {
                case MouseButton.Left:
                    await MouseHelper.Input.ClickAsync(x, y, windowTitle, windowClassName);
                    break;
                case MouseButton.Right:
                    await MouseHelper.Input.RightClickAsync(x, y, windowTitle, windowClassName);
                    break;
                case MouseButton.Middle:
                    await MouseHelper.Input.MiddleClickAsync(x, y, windowTitle, windowClassName);
                    break;
                default:
                    throw new ArgumentException($"サポートされていないマウスボタン: {button}");
            }
        }
    }

    /// <summary>
    /// ホットキープラグインコマンド
    /// </summary>
    public class HotkeyPluginCommand : PluginCommandBase
    {
        public new IHotkeyCommandSettings? Settings => base.Settings as IHotkeyCommandSettings;

        public HotkeyPluginCommand(MacroPanels.Plugin.IPluginInfo pluginInfo, MacroPanels.Plugin.IPluginCommandInfo commandInfo, 
            MacroPanels.Command.Interface.ICommand? parent = null, object? settings = null) 
            : base(pluginInfo, commandInfo, parent, settings)
        {
        }

        protected override async Task<bool> DoExecutePluginAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            var keyDescription = BuildKeyDescription(settings);
            LogMessage($"ホットキー実行: {keyDescription}");
            
            await Task.Run(() => KeyHelper.Input.KeyPress(
                settings.Key, settings.Ctrl, settings.Alt, settings.Shift,
                settings.WindowTitle, settings.WindowClassName));

            LogMessage("ホットキーを実行しました。");
            return true;
        }

        private string BuildKeyDescription(IHotkeyCommandSettings settings)
        {
            var parts = new List<string>();
            if (settings.Ctrl) parts.Add("Ctrl");
            if (settings.Alt) parts.Add("Alt");
            if (settings.Shift) parts.Add("Shift");
            parts.Add(settings.Key.ToString());
            
            return string.Join(" + ", parts);
        }
    }

    /// <summary>
    /// 待機プラグインコマンド
    /// </summary>
    public class WaitPluginCommand : PluginCommandBase
    {
        public new IWaitCommandSettings? Settings => base.Settings as IWaitCommandSettings;

        public WaitPluginCommand(MacroPanels.Plugin.IPluginInfo pluginInfo, MacroPanels.Plugin.IPluginCommandInfo commandInfo, 
            MacroPanels.Command.Interface.ICommand? parent = null, object? settings = null) 
            : base(pluginInfo, commandInfo, parent, settings)
        {
        }

        protected override void ValidatePluginSettings()
        {
            var settings = Settings;
            if (settings != null)
            {
                if (settings.Wait < 0)
                    throw new ArgumentException("Waitは0以上である必要があります");
            }
        }

        protected override async Task<bool> DoExecutePluginAsync(CancellationToken cancellationToken)
        {
            var settings = Settings;
            if (settings == null) return false;

            LogMessage($"待機開始: {settings.Wait}ms");
            
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < settings.Wait)
            {
                if (cancellationToken.IsCancellationRequested) return false;

                ReportProgress(stopwatch.ElapsedMilliseconds, settings.Wait);
                await Task.Delay(50, cancellationToken);
            }

            LogMessage("待機が完了しました。");
            return true;
        }
    }

    #endregion

    /// <summary>
    /// サンプルコマンドプラグイン
    /// </summary>
    public class SampleCommandPlugin : MacroPanels.Plugin.ICommandPlugin
    {
        private readonly MacroPanels.Plugin.IPluginInfo _pluginInfo;
        private readonly PluginCommandFactoryBase _commandFactory;

        public MacroPanels.Plugin.IPluginInfo Info => _pluginInfo;

        public SampleCommandPlugin()
        {
            _pluginInfo = new SamplePluginInfo();
            _commandFactory = new SampleCommandFactory(_pluginInfo);
        }

        public async Task InitializeAsync()
        {
            // プラグイン初期化処理
            await Task.CompletedTask;
        }

        public async Task ShutdownAsync()
        {
            // プラグイン終了処理
            await Task.CompletedTask;
        }

        public IEnumerable<MacroPanels.Plugin.IPluginCommandInfo> GetAvailableCommands()
        {
            return _commandFactory.GetAvailableCommands();
        }

        public MacroPanels.Command.Interface.ICommand CreateCommand(string commandId, MacroPanels.Command.Interface.ICommand? parent, object? settings)
        {
            return _commandFactory.CreateCommand(commandId, parent, settings);
        }

        public Type? GetCommandSettingsType(string commandId)
        {
            return _commandFactory.GetCommandSettingsType(commandId);
        }

        public bool IsCommandAvailable(string commandId)
        {
            return _commandFactory.IsCommandAvailable(commandId);
        }
    }

    /// <summary>
    /// サンプルプラグイン情報
    /// </summary>
    public class SamplePluginInfo : MacroPanels.Plugin.IPluginInfo
    {
        public string Id => "SamplePlugin";
        public string Name => "サンプルプラグイン";
        public string Version => "1.0.0";
        public string Description => "プラグインシステムのサンプル実装";
        public string Author => "AutoTool";
        public DateTime LoadedAt { get; set; } = DateTime.UtcNow;
        public MacroPanels.Plugin.PluginStatus Status { get; set; } = MacroPanels.Plugin.PluginStatus.NotLoaded;
    }

    /// <summary>
    /// サンプルコマンドファクトリー
    /// </summary>
    public class SampleCommandFactory : PluginCommandFactoryBase
    {
        private readonly List<MacroPanels.Plugin.IPluginCommandInfo> _availableCommands;

        public SampleCommandFactory(MacroPanels.Plugin.IPluginInfo pluginInfo) : base(pluginInfo)
        {
            _availableCommands = new List<MacroPanels.Plugin.IPluginCommandInfo>
            {
                PluginCommandHelper.CreateCommandInfo(
                    "SampleMessage", 
                    "メッセージ表示", 
                    "カスタムメッセージを表示して待機する", 
                    pluginInfo.Id,
                    typeof(SamplePluginCommand),
                    typeof(ISampleCommandSettings),
                    "サンプル"),

                PluginCommandHelper.CreateCommandInfo(
                    "FileOperation", 
                    "ファイル操作", 
                    "ファイルのコピー、移動、削除を行う", 
                    pluginInfo.Id,
                    typeof(FileOperationCommand),
                    typeof(IFileOperationSettings),
                    "ファイル"),

                PluginCommandHelper.CreateCommandInfo(
                    "WaitImage", 
                    "画像待機", 
                    "指定した画像が見つかるまで待機する", 
                    pluginInfo.Id,
                    typeof(WaitImagePluginCommand),
                    typeof(IWaitImageCommandSettings),
                    "画像"),

                PluginCommandHelper.CreateCommandInfo(
                    "ClickImage", 
                    "画像クリック", 
                    "指定した画像を見つけてクリックする", 
                    pluginInfo.Id,
                    typeof(ClickImagePluginCommand),
                    typeof(IClickImageCommandSettings),
                    "画像"),

                PluginCommandHelper.CreateCommandInfo(
                    "Hotkey", 
                    "ホットキー", 
                    "キーボードショートカットを送信する", 
                    pluginInfo.Id,
                    typeof(HotkeyPluginCommand),
                    typeof(IHotkeyCommandSettings),
                    "キーボード"),

                PluginCommandHelper.CreateCommandInfo(
                    "Wait", 
                    "待機", 
                    "指定した時間だけ待機する", 
                    pluginInfo.Id,
                    typeof(WaitPluginCommand),
                    typeof(IWaitCommandSettings),
                    "制御")
            };
        }

        public override IEnumerable<MacroPanels.Plugin.IPluginCommandInfo> GetAvailableCommands()
        {
            return _availableCommands;
        }

        public override MacroPanels.Command.Interface.ICommand CreateCommand(string commandId, MacroPanels.Command.Interface.ICommand? parent, object? settings)
        {
            var commandInfo = _availableCommands.FirstOrDefault(c => c.Id == commandId);
            if (commandInfo == null)
                throw new ArgumentException($"未知のコマンドID: {commandId}");

            return commandId switch
            {
                "SampleMessage" => new SamplePluginCommand(PluginInfo, commandInfo, parent, settings),
                "FileOperation" => new FileOperationCommand(PluginInfo, commandInfo, parent, settings),
                "WaitImage" => new WaitImagePluginCommand(PluginInfo, commandInfo, parent, settings),
                "ClickImage" => new ClickImagePluginCommand(PluginInfo, commandInfo, parent, settings),
                "Hotkey" => new HotkeyPluginCommand(PluginInfo, commandInfo, parent, settings),
                "Wait" => new WaitPluginCommand(PluginInfo, commandInfo, parent, settings),
                _ => throw new ArgumentException($"未対応のコマンドID: {commandId}")
            };
        }

        public override Type? GetCommandSettingsType(string commandId)
        {
            return commandId switch
            {
                "SampleMessage" => typeof(ISampleCommandSettings),
                "FileOperation" => typeof(IFileOperationSettings),
                "WaitImage" => typeof(IWaitImageCommandSettings),
                "ClickImage" => typeof(IClickImageCommandSettings),
                "Hotkey" => typeof(IHotkeyCommandSettings),
                "Wait" => typeof(IWaitCommandSettings),
                _ => null
            };
        }
    }
}