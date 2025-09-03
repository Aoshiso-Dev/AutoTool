using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Plugin;

namespace AutoTool.SamplePlugins
{
    /// <summary>
    /// サンプルコマンドプラグイン
    /// MacroPanels依存を削除し、AutoTool統合版のみ使用
    /// </summary>

    #region サンプル設定インターフェース

    /// <summary>
    /// サンプルコマンド設定インターフェース
    /// </summary>
    public interface ISampleCommandSettings
    {
        string SampleText { get; set; }
        int SampleNumber { get; set; }
        bool SampleFlag { get; set; }
    }

    /// <summary>
    /// ファイル操作設定インターフェース
    /// </summary>
    public interface IFileOperationSettings
    {
        string FilePath { get; set; }
        string Operation { get; set; }
        bool CreateIfNotExists { get; set; }
    }

    #endregion

    #region サンプルコマンド実装

    /// <summary>
    /// サンプルプラグインコマンド
    /// </summary>
    public class SamplePluginCommand : PluginCommandBase
    {
        public ISampleCommandSettings? Settings { get; set; }

        public SamplePluginCommand(IPluginInfo pluginInfo, IPluginCommandInfo commandInfo)
            : base(pluginInfo, commandInfo)
        {
        }

        public override void Execute()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"サンプルコマンド実行: {Settings?.SampleText ?? "デフォルトテキスト"}");
                
                if (Settings != null)
                {
                    System.Diagnostics.Debug.WriteLine($"  - サンプル番号: {Settings.SampleNumber}");
                    System.Diagnostics.Debug.WriteLine($"  - サンプルフラグ: {Settings.SampleFlag}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"サンプルコマンド実行エラー: {ex.Message}");
            }
        }

        public override bool Validate()
        {
            return !string.IsNullOrEmpty(Settings?.SampleText);
        }

        protected override string GetDescription()
        {
            return $"サンプルコマンド: {Settings?.SampleText ?? "未設定"}";
        }
    }

    /// <summary>
    /// ファイル操作コマンド
    /// </summary>
    public class FileOperationCommand : PluginCommandBase
    {
        public IFileOperationSettings? Settings { get; set; }

        public FileOperationCommand(IPluginInfo pluginInfo, IPluginCommandInfo commandInfo)
            : base(pluginInfo, commandInfo)
        {
        }

        public override void Execute()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"ファイル操作実行: {Settings?.Operation ?? "不明"} - {Settings?.FilePath ?? "パス未指定"}");
                
                // ファイル操作の簡易実装
                if (Settings != null && !string.IsNullOrEmpty(Settings.FilePath))
                {
                    switch (Settings.Operation?.ToLower())
                    {
                        case "create":
                            if (Settings.CreateIfNotExists && !System.IO.File.Exists(Settings.FilePath))
                            {
                                System.IO.File.WriteAllText(Settings.FilePath, $"作成日時: {DateTime.Now}");
                                System.Diagnostics.Debug.WriteLine($"  - ファイル作成完了: {Settings.FilePath}");
                            }
                            break;
                        case "delete":
                            if (System.IO.File.Exists(Settings.FilePath))
                            {
                                System.IO.File.Delete(Settings.FilePath);
                                System.Diagnostics.Debug.WriteLine($"  - ファイル削除完了: {Settings.FilePath}");
                            }
                            break;
                        default:
                            System.Diagnostics.Debug.WriteLine($"  - 未サポート操作: {Settings.Operation}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ファイル操作エラー: {ex.Message}");
            }
        }

        public override bool Validate()
        {
            return !string.IsNullOrEmpty(Settings?.FilePath) && !string.IsNullOrEmpty(Settings?.Operation);
        }

        protected override string GetDescription()
        {
            return $"ファイル操作: {Settings?.Operation ?? "未設定"} - {System.IO.Path.GetFileName(Settings?.FilePath) ?? "ファイル未設定"}";
        }
    }

    #endregion

    #region プラグイン情報

    /// <summary>
    /// サンプルプラグイン情報
    /// </summary>
    public class SamplePluginInfo : IPluginInfo
    {
        public string Id { get; } = "SampleCommandPlugin";
        public string Name { get; } = "サンプルコマンドプラグイン";
        public string Version { get; } = "1.0.0";
        public string Description { get; } = "サンプルプラグインです";
        public string Author { get; } = "AutoTool Development Team";
        public DateTime LoadedAt { get; set; } = DateTime.Now;
        public PluginStatus Status { get; set; } = PluginStatus.NotLoaded;
    }

    #endregion

    #region プラグインメインクラス

    /// <summary>
    /// サンプルコマンドプラグイン
    /// </summary>
    public class SampleCommandPlugin : IPlugin
    {
        private readonly IPluginInfo _pluginInfo;

        public IPluginInfo Info => _pluginInfo;

        public SampleCommandPlugin()
        {
            _pluginInfo = new SamplePluginInfo();
        }

        public async Task InitializeAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"{Info.Name} 初期化開始");
                
                // 非同期初期化処理のシミュレーション
                await Task.Delay(100);
                
                _pluginInfo.Status = PluginStatus.Active;
                System.Diagnostics.Debug.WriteLine($"{Info.Name} 初期化完了");
            }
            catch (Exception ex)
            {
                _pluginInfo.Status = PluginStatus.Error;
                System.Diagnostics.Debug.WriteLine($"{Info.Name} 初期化エラー: {ex.Message}");
                throw;
            }
        }

        public async Task ShutdownAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"{Info.Name} 終了処理開始");
                
                // 非同期終了処理のシミュレーション
                await Task.Delay(50);
                
                _pluginInfo.Status = PluginStatus.NotLoaded;
                System.Diagnostics.Debug.WriteLine($"{Info.Name} 終了処理完了");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"{Info.Name} 終了処理エラー: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 利用可能なコマンド一覧を取得
        /// </summary>
        public IEnumerable<IPluginCommandInfo> GetAvailableCommands()
        {
            return new[]
            {
                new PluginCommandInfo
                {
                    Id = "SampleCommand",
                    Name = "サンプルコマンド",
                    Description = "サンプルコマンドです",
                    Category = "サンプル",
                    PluginId = Info.Id,
                    CommandType = typeof(SamplePluginCommand),
                    SettingsType = typeof(ISampleCommandSettings)
                },
                new PluginCommandInfo
                {
                    Id = "FileOperation",
                    Name = "ファイル操作",
                    Description = "ファイル操作コマンドです",
                    Category = "ファイル",
                    PluginId = Info.Id,
                    CommandType = typeof(FileOperationCommand),
                    SettingsType = typeof(IFileOperationSettings)
                }
            };
        }

        /// <summary>
        /// コマンドを作成
        /// </summary>
        public object CreateCommand(string commandId, object? parent, object? settings)
        {
            var commandInfo = GetAvailableCommands().FirstOrDefault(c => c.Id == commandId);
            if (commandInfo == null)
            {
                throw new ArgumentException($"コマンドが見つかりません: {commandId}");
            }

            var command = Activator.CreateInstance(commandInfo.CommandType, Info, commandInfo);
            
            // 設定適用
            if (settings != null && command != null)
            {
                var settingsProperty = commandInfo.CommandType.GetProperty("Settings");
                settingsProperty?.SetValue(command, settings);
            }

            return command ?? throw new InvalidOperationException($"コマンドの作成に失敗しました: {commandId}");
        }

        /// <summary>
        /// コマンド設定の型を取得
        /// </summary>
        public Type? GetCommandSettingsType(string commandId)
        {
            var commandInfo = GetAvailableCommands().FirstOrDefault(c => c.Id == commandId);
            return commandInfo?.SettingsType;
        }

        /// <summary>
        /// コマンドが利用可能かどうか
        /// </summary>
        public bool IsCommandAvailable(string commandId)
        {
            return GetAvailableCommands().Any(c => c.Id == commandId);
        }
    }

    #endregion
}