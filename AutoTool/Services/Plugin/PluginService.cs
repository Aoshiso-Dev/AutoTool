using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Configuration;
using MacroPanels.Command.Interface;
using MacroPanels.Plugin;

namespace AutoTool.Services.Plugin
{
    /// <summary>
    /// プラグインサービスの実装
    /// </summary>
    public class PluginService : MacroPanels.Plugin.IPluginService
    {
        private readonly ILogger<PluginService> _logger;
        private readonly IConfigurationService _configurationService;
        private readonly ConcurrentDictionary<string, LoadedPlugin> _loadedPlugins;
        private readonly ConcurrentDictionary<string, MacroPanels.Plugin.IPluginCommandInfo> _availableCommands;
        private readonly string _pluginsDirectory;

        public event EventHandler<MacroPanels.Plugin.PluginLoadedEventArgs>? PluginLoaded;
        public event EventHandler<MacroPanels.Plugin.PluginUnloadedEventArgs>? PluginUnloaded;

        public PluginService(ILogger<PluginService> logger, IConfigurationService configurationService)
        {
            _logger = logger;
            _configurationService = configurationService;
            _loadedPlugins = new ConcurrentDictionary<string, LoadedPlugin>();
            _availableCommands = new ConcurrentDictionary<string, MacroPanels.Plugin.IPluginCommandInfo>();
            
            _pluginsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AutoTool", "Plugins");

            EnsurePluginDirectoryExists();
            _logger.LogInformation("PluginService 初期化完了: {PluginDirectory}", _pluginsDirectory);
        }

        /// <summary>
        /// プラグインを読み込み
        /// </summary>
        public async Task LoadPluginAsync(string pluginPath)
        {
            if (string.IsNullOrWhiteSpace(pluginPath))
                throw new ArgumentException("Plugin path cannot be null or empty", nameof(pluginPath));

            if (!File.Exists(pluginPath))
                throw new FileNotFoundException($"Plugin file not found: {pluginPath}");

            try
            {
                _logger.LogInformation("プラグインを読み込み中: {PluginPath}", pluginPath);

                // アセンブリ読み込みコンテキストを作成
                var loadContext = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(pluginPath), true);
                var assembly = loadContext.LoadFromAssemblyPath(pluginPath);

                // IPlugin実装型を検索
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(MacroPanels.Plugin.IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                if (!pluginTypes.Any())
                {
                    _logger.LogWarning("プラグインアセンブリにIPlugin実装型が見つかりません: {PluginPath}", pluginPath);
                    return;
                }

                foreach (var pluginType in pluginTypes)
                {
                    await LoadPluginInstance(pluginType, loadContext, pluginPath);
                }

                _logger.LogInformation("プラグイン読み込み完了: {PluginPath}, 読み込み済み型数: {TypeCount}", 
                    pluginPath, pluginTypes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "プラグイン読み込み中にエラーが発生: {PluginPath}", pluginPath);
                throw;
            }
        }

        /// <summary>
        /// 全てのプラグインを読み込み
        /// </summary>
        public async Task LoadAllPluginsAsync()
        {
            try
            {
                _logger.LogInformation("全プラグインの読み込みを開始");

                if (!Directory.Exists(_pluginsDirectory))
                {
                    _logger.LogInformation("プラグインディレクトリが存在しません: {Directory}", _pluginsDirectory);
                    return;
                }

                var pluginFiles = Directory.GetFiles(_pluginsDirectory, "*.dll", SearchOption.AllDirectories);
                _logger.LogInformation("プラグインファイル発見: {Count}個", pluginFiles.Length);

                var loadTasks = pluginFiles.Select(LoadPluginAsync);
                await Task.WhenAll(loadTasks);

                _logger.LogInformation("全プラグインの読み込み完了: {LoadedCount}個", _loadedPlugins.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "全プラグイン読み込み中にエラーが発生しました");
                throw;
            }
        }

        /// <summary>
        /// プラグインをアンロード
        /// </summary>
        public async Task UnloadPluginAsync(string pluginId)
        {
            if (string.IsNullOrWhiteSpace(pluginId))
                throw new ArgumentException("Plugin ID cannot be null or empty", nameof(pluginId));

            try
            {
                if (_loadedPlugins.TryRemove(pluginId, out var loadedPlugin))
                {
                    _logger.LogInformation("プラグインをアンロード中: {PluginId}", pluginId);

                    loadedPlugin.Info.Status = MacroPanels.Plugin.PluginStatus.Unloading;

                    // コマンドプラグインの場合、コマンド情報も削除
                    if (loadedPlugin.Instance is MacroPanels.Plugin.ICommandPlugin commandPlugin)
                    {
                        var commandsToRemove = _availableCommands.Where(kvp => kvp.Value.PluginId == pluginId)
                            .Select(kvp => kvp.Key)
                            .ToList();

                        foreach (var commandId in commandsToRemove)
                        {
                            _availableCommands.TryRemove(commandId, out _);
                        }

                        _logger.LogDebug("プラグインコマンドを削除: {CommandCount}個", commandsToRemove.Count);
                    }

                    // プラグインのシャットダウン処理
                    try
                    {
                        await loadedPlugin.Instance.ShutdownAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "プラグインシャットダウン中にエラー: {PluginId}", pluginId);
                    }

                    // アセンブリ読み込みコンテキストをアンロード
                    loadedPlugin.LoadContext?.Unload();

                    PluginUnloaded?.Invoke(this, new MacroPanels.Plugin.PluginUnloadedEventArgs(pluginId));
                    _logger.LogInformation("プラグインアンロード完了: {PluginId}", pluginId);
                }
                else
                {
                    _logger.LogWarning("アンロード対象のプラグインが見つかりません: {PluginId}", pluginId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "プラグインアンロード中にエラー: {PluginId}", pluginId);
                throw;
            }
        }

        /// <summary>
        /// 読み込み済みプラグイン一覧を取得
        /// </summary>
        public IEnumerable<MacroPanels.Plugin.IPluginInfo> GetLoadedPlugins()
        {
            return _loadedPlugins.Values.Select(p => p.Info).ToList();
        }

        /// <summary>
        /// プラグインを取得
        /// </summary>
        public T? GetPlugin<T>(string pluginId) where T : class, MacroPanels.Plugin.IPlugin
        {
            if (_loadedPlugins.TryGetValue(pluginId, out var loadedPlugin))
            {
                return loadedPlugin.Instance as T;
            }
            return null;
        }

        /// <summary>
        /// コマンドプラグインからコマンドを作成
        /// </summary>
        public ICommand? CreatePluginCommand(string pluginId, string commandId, ICommand? parent, object? settings)
        {
            try
            {
                var plugin = GetPlugin<MacroPanels.Plugin.ICommandPlugin>(pluginId);
                if (plugin == null)
                {
                    _logger.LogWarning("コマンドプラグインが見つかりません: {PluginId}", pluginId);
                    return null;
                }

                if (!plugin.IsCommandAvailable(commandId))
                {
                    _logger.LogWarning("コマンドが利用できません: {PluginId}.{CommandId}", pluginId, commandId);
                    return null;
                }

                var command = plugin.CreateCommand(commandId, parent, settings);
                _logger.LogDebug("プラグインコマンド作成成功: {PluginId}.{CommandId}", pluginId, commandId);
                
                return command;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "プラグインコマンド作成エラー: {PluginId}.{CommandId}", pluginId, commandId);
                return null;
            }
        }

        /// <summary>
        /// 利用可能なプラグインコマンドを取得
        /// </summary>
        public IEnumerable<MacroPanels.Plugin.IPluginCommandInfo> GetAvailablePluginCommands()
        {
            return _availableCommands.Values.ToList();
        }

        /// <summary>
        /// プラグインインスタンスを読み込み
        /// </summary>
        private async Task LoadPluginInstance(Type pluginType, AssemblyLoadContext loadContext, string pluginPath)
        {
            try
            {
                _logger.LogDebug("プラグインインスタンスを作成: {PluginType}", pluginType.FullName);

                var pluginInstance = Activator.CreateInstance(pluginType) as MacroPanels.Plugin.IPlugin;
                if (pluginInstance == null)
                {
                    _logger.LogWarning("プラグインインスタンスの作成に失敗: {PluginType}", pluginType.FullName);
                    return;
                }

                var pluginInfo = new PluginInfo
                {
                    Id = pluginInstance.Info.Id,
                    Name = pluginInstance.Info.Name,
                    Version = pluginInstance.Info.Version,
                    Description = pluginInstance.Info.Description,
                    Author = pluginInstance.Info.Author,
                    LoadedAt = DateTime.UtcNow,
                    Status = MacroPanels.Plugin.PluginStatus.Initializing
                };

                var loadedPlugin = new LoadedPlugin
                {
                    Instance = pluginInstance,
                    Info = pluginInfo,
                    LoadContext = loadContext,
                    AssemblyPath = pluginPath
                };

                // プラグイン初期化
                await pluginInstance.InitializeAsync();
                pluginInfo.Status = MacroPanels.Plugin.PluginStatus.Active;

                // コマンドプラグインの場合、コマンド情報を登録
                if (pluginInstance is MacroPanels.Plugin.ICommandPlugin commandPlugin)
                {
                    RegisterCommandPluginCommands(commandPlugin);
                }

                _loadedPlugins[pluginInfo.Id] = loadedPlugin;

                PluginLoaded?.Invoke(this, new MacroPanels.Plugin.PluginLoadedEventArgs(pluginInfo));
                _logger.LogInformation("プラグインインスタンス作成完了: {PluginId} ({PluginName})", 
                    pluginInfo.Id, pluginInfo.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "プラグインインスタンス作成エラー: {PluginType}", pluginType.FullName);
                throw;
            }
        }

        /// <summary>
        /// コマンドプラグインのコマンドを登録
        /// </summary>
        private void RegisterCommandPluginCommands(MacroPanels.Plugin.ICommandPlugin commandPlugin)
        {
            try
            {
                var commands = commandPlugin.GetAvailableCommands();
                foreach (var commandInfo in commands)
                {
                    var fullCommandId = $"{commandInfo.PluginId}.{commandInfo.Id}";
                    _availableCommands[fullCommandId] = commandInfo;
                    
                    _logger.LogDebug("プラグインコマンド登録: {FullCommandId} ({Name})", 
                        fullCommandId, commandInfo.Name);
                }

                _logger.LogInformation("コマンドプラグイン登録完了: {PluginId}, コマンド数: {CommandCount}", 
                    commandPlugin.Info.Id, commands.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "コマンドプラグイン登録エラー: {PluginId}", commandPlugin.Info.Id);
            }
        }

        /// <summary>
        /// プラグインディレクトリが存在することを確認
        /// </summary>
        private void EnsurePluginDirectoryExists()
        {
            if (!Directory.Exists(_pluginsDirectory))
            {
                Directory.CreateDirectory(_pluginsDirectory);
                _logger.LogDebug("プラグインディレクトリを作成: {Directory}", _pluginsDirectory);
            }
        }

        /// <summary>
        /// 読み込み済みプラグイン情報
        /// </summary>
        private class LoadedPlugin
        {
            public MacroPanels.Plugin.IPlugin Instance { get; set; } = null!;
            public PluginInfo Info { get; set; } = null!;
            public AssemblyLoadContext? LoadContext { get; set; }
            public string AssemblyPath { get; set; } = string.Empty;
        }

        /// <summary>
        /// プラグイン情報の実装
        /// </summary>
        private class PluginInfo : MacroPanels.Plugin.IPluginInfo
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Author { get; set; } = string.Empty;
            public DateTime LoadedAt { get; set; }
            public MacroPanels.Plugin.PluginStatus Status { get; set; }
        }
    }
}