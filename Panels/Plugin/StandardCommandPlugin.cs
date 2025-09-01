using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Linq;
using MacroPanels.Command.Interface;
using MacroPanels.Command.Class;
using MacroPanels.Plugin;
using OpenCVHelper;
using KeyHelper;
using MouseHelper;
using WpfCommand = System.Windows.Input.ICommand;
using MacroCommand = MacroPanels.Command.Interface.ICommand;

namespace MacroPanels.Plugin.CommandPlugins
{
    /// <summary>
    /// プラグインコマンドの基底クラス
    /// </summary>
    public abstract class PluginCommandBase : BaseCommand
    {
        /// <summary>
        /// プラグイン情報
        /// </summary>
        public IPluginInfo PluginInfo { get; }

        /// <summary>
        /// コマンド情報
        /// </summary>
        public IPluginCommandInfo CommandInfo { get; }

        protected PluginCommandBase(IPluginInfo pluginInfo, IPluginCommandInfo commandInfo, 
            MacroCommand? parent = null, object? settings = null) 
            : base(parent, settings)
        {
            PluginInfo = pluginInfo ?? throw new ArgumentNullException(nameof(pluginInfo));
            CommandInfo = commandInfo ?? throw new ArgumentNullException(nameof(commandInfo));
            Description = $"[{pluginInfo.Name}] {commandInfo.Name}";
        }

        /// <summary>
        /// プラグインコマンドの実際の実行処理
        /// 派生クラスで実装する
        /// </summary>
        protected abstract Task<bool> DoExecutePluginAsync(CancellationToken cancellationToken);

        /// <summary>
        /// BaseCommandの実行処理をオーバーライド
        /// </summary>
        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                LogMessage($"プラグインコマンド開始: {PluginInfo.Name}.{CommandInfo.Name}");
                
                var result = await DoExecutePluginAsync(cancellationToken);
                
                LogMessage($"プラグインコマンド終了: {PluginInfo.Name}.{CommandInfo.Name} - {(result ? "成功" : "失敗")}");
                
                return result;
            }
            catch (Exception ex)
            {
                LogMessage($"? プラグインコマンドエラー: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// プラグイン設定を取得（型安全）
        /// </summary>
        protected T? GetPluginSettings<T>() where T : class
        {
            return Settings as T;
        }

        /// <summary>
        /// プラグイン設定の検証
        /// </summary>
        protected virtual void ValidatePluginSettings()
        {
            // 基底クラスでは何もしない（派生クラスでオーバーライド）
        }

        /// <summary>
        /// ファイル検証をオーバーライド
        /// </summary>
        protected override void ValidateFiles()
        {
            base.ValidateFiles();
            ValidatePluginSettings();
        }
    }

    #region サンプルコマンド

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

        public SamplePluginCommand(IPluginInfo pluginInfo, IPluginCommandInfo commandInfo, 
            MacroCommand? parent = null, object? settings = null) 
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

    #endregion

    #region ファイル操作コマンド

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

        public FileOperationCommand(IPluginInfo pluginInfo, IPluginCommandInfo commandInfo, 
            MacroCommand? parent = null, object? settings = null) 
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

    #endregion

    /// <summary>
    /// 標準コマンドプラグインの実装
    /// </summary>
    public class StandardCommandPlugin : ICommandPlugin
    {
        private readonly IPluginInfo _pluginInfo;
        private readonly List<IPluginCommandInfo> _availableCommands;

        public IPluginInfo Info => _pluginInfo;

        public StandardCommandPlugin()
        {
            _pluginInfo = new StandardPluginInfo();
            _availableCommands = CreateCommandList();
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

        public IEnumerable<IPluginCommandInfo> GetAvailableCommands()
        {
            return _availableCommands;
        }

        public MacroCommand CreateCommand(string commandId, MacroCommand? parent, object? settings)
        {
            var commandInfo = _availableCommands.FirstOrDefault(c => c.Id == commandId);
            if (commandInfo == null)
                throw new ArgumentException($"未知のコマンドID: {commandId}");

            return commandId switch
            {
                "SampleMessage" => new SamplePluginCommand(_pluginInfo, commandInfo, parent, settings),
                "FileOperation" => new FileOperationCommand(_pluginInfo, commandInfo, parent, settings),
                _ => throw new ArgumentException($"未対応のコマンドID: {commandId}")
            };
        }

        public Type? GetCommandSettingsType(string commandId)
        {
            return commandId switch
            {
                "SampleMessage" => typeof(ISampleCommandSettings),
                "FileOperation" => typeof(IFileOperationSettings),
                _ => null
            };
        }

        public bool IsCommandAvailable(string commandId)
        {
            return _availableCommands.Any(c => c.Id == commandId);
        }

        private List<IPluginCommandInfo> CreateCommandList()
        {
            return new List<IPluginCommandInfo>
            {
                new PluginCommandInfo
                {
                    Id = "SampleMessage",
                    Name = "メッセージ表示",
                    Description = "カスタムメッセージを表示して待機する",
                    Category = "サンプル",
                    PluginId = _pluginInfo.Id,
                    CommandType = typeof(SamplePluginCommand),
                    SettingsType = typeof(ISampleCommandSettings)
                },

                new PluginCommandInfo
                {
                    Id = "FileOperation",
                    Name = "ファイル操作",
                    Description = "ファイルのコピー、移動、削除を行う",
                    Category = "ファイル",
                    PluginId = _pluginInfo.Id,
                    CommandType = typeof(FileOperationCommand),
                    SettingsType = typeof(IFileOperationSettings)
                }
            };
        }
    }

    /// <summary>
    /// 標準プラグイン情報
    /// </summary>
    public class StandardPluginInfo : IPluginInfo
    {
        public string Id => "StandardCommands";
        public string Name => "標準コマンドプラグイン";
        public string Version => "1.0.0";
        public string Description => "基本的なマクロコマンド（ファイル操作、サンプル等）";
        public string Author => "MacroPanels";
        public DateTime LoadedAt { get; set; } = DateTime.UtcNow;
        public PluginStatus Status { get; set; } = PluginStatus.NotLoaded;
    }
}