using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AutoTool.Services.Plugin;

namespace AutoTool.Services.Plugin
{
    /// <summary>
    /// Phase 5完全統合版：プラグインサービス
    /// MacroPanels依存を削除し、AutoTool統合版のみ使用
    /// </summary>
    public class PluginService : AutoTool.Services.Plugin.IPluginService
    {
        private readonly ILogger<PluginService> _logger;
        private readonly ConcurrentDictionary<string, IPlugin> _loadedPlugins;
        private readonly ConcurrentDictionary<string, IPluginCommandInfo> _availableCommands;

        // Phase 5統合版イベント
        public event EventHandler<PluginLoadedEventArgs>? PluginLoaded;
        public event EventHandler<PluginUnloadedEventArgs>? PluginUnloaded;

        public PluginService(ILogger<PluginService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loadedPlugins = new ConcurrentDictionary<string, IPlugin>();
            _availableCommands = new ConcurrentDictionary<string, IPluginCommandInfo>();
        }

        public async Task LoadPluginAsync(string pluginPath)
        {
            try
            {
                _logger.LogInformation("Phase 5統合版プラグイン読み込み開始: {PluginPath}", pluginPath);

                if (string.IsNullOrEmpty(pluginPath) || !File.Exists(pluginPath))
                {
                    throw new FileNotFoundException($"プラグインファイルが見つかりません: {pluginPath}");
                }

                var assembly = Assembly.LoadFrom(pluginPath);
                var pluginTypes = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && typeof(IPlugin).IsAssignableFrom(t))
                    .ToList();

                foreach (var pluginType in pluginTypes)
                {
                    try
                    {
                        var plugin = (IPlugin)Activator.CreateInstance(pluginType)!;
                        await plugin.InitializeAsync();

                        _loadedPlugins.TryAdd(plugin.Info.Id, plugin);
                        
                        // イベント発火
                        PluginLoaded?.Invoke(this, new PluginLoadedEventArgs(plugin.Info));
                        
                        _logger.LogInformation("Phase 5統合版プラグイン読み込み完了: {PluginId}", plugin.Info.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "プラグインインスタンス作成エラー: {PluginType}", pluginType.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Phase 5統合版プラグイン読み込みエラー: {PluginPath}", pluginPath);
                throw;
            }
        }

        public async Task LoadAllPluginsAsync()
        {
            try
            {
                _logger.LogInformation("Phase 5統合版全プラグイン読み込み開始");

                var pluginDirectory = Path.Combine(AppContext.BaseDirectory, "Plugins");
                if (!Directory.Exists(pluginDirectory))
                {
                    _logger.LogWarning("プラグインディレクトリが存在しません: {PluginDirectory}", pluginDirectory);
                    return;
                }

                var pluginFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.AllDirectories);
                foreach (var pluginFile in pluginFiles)
                {
                    try
                    {
                        await LoadPluginAsync(pluginFile);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "プラグインファイル読み込みスキップ: {PluginFile}", pluginFile);
                    }
                }

                _logger.LogInformation("Phase 5統合版全プラグイン読み込み完了: {Count}個", _loadedPlugins.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Phase 5統合版全プラグイン読み込みエラー");
                throw;
            }
        }

        public async Task UnloadPluginAsync(string pluginId)
        {
            try
            {
                if (_loadedPlugins.TryRemove(pluginId, out var plugin))
                {
                    await plugin.ShutdownAsync();
                    
                    // プラグイン関連のコマンドも削除
                    var commandsToRemove = _availableCommands
                        .Where(kvp => kvp.Value.PluginId == pluginId)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var commandId in commandsToRemove)
                    {
                        _availableCommands.TryRemove(commandId, out _);
                    }

                    // イベント発火
                    PluginUnloaded?.Invoke(this, new PluginUnloadedEventArgs(pluginId));
                    
                    _logger.LogInformation("Phase 5統合版プラグインアンロード完了: {PluginId}", pluginId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Phase 5統合版プラグインアンロードエラー: {PluginId}", pluginId);
                throw;
            }
        }

        public IEnumerable<IPluginInfo> GetLoadedPlugins()
        {
            return _loadedPlugins.Values.Select(p => p.Info).ToList();
        }

        public T? GetPlugin<T>(string pluginId) where T : class, IPlugin
        {
            if (_loadedPlugins.TryGetValue(pluginId, out var plugin))
            {
                return plugin as T;
            }
            return null;
        }

        public object? CreatePluginCommand(string pluginId, string commandId, object? parent, object? settings)
        {
            try
            {
                _logger.LogDebug("Phase 5統合版プラグインコマンド作成: {PluginId}.{CommandId}", pluginId, commandId);

                if (_availableCommands.TryGetValue($"{pluginId}.{commandId}", out var commandInfo))
                {
                    var command = Activator.CreateInstance(commandInfo.CommandType);
                    _logger.LogDebug("Phase 5統合版プラグインコマンド作成完了: {CommandType}", commandInfo.CommandType.Name);
                    return command;
                }

                _logger.LogWarning("Phase 5統合版プラグインコマンドが見つかりません: {PluginId}.{CommandId}", pluginId, commandId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Phase 5統合版プラグインコマンド作成エラー: {PluginId}.{CommandId}", pluginId, commandId);
                return null;
            }
        }

        public IEnumerable<IPluginCommandInfo> GetAvailablePluginCommands()
        {
            return _availableCommands.Values.ToList();
        }

        public void Dispose()
        {
            try
            {
                _logger.LogInformation("Phase 5統合版PluginService のDispose処理を開始します");

                var unloadTasks = _loadedPlugins.Keys.Select(pluginId => UnloadPluginAsync(pluginId));
                Task.WaitAll(unloadTasks.ToArray(), TimeSpan.FromSeconds(30));

                _loadedPlugins.Clear();
                _availableCommands.Clear();

                _logger.LogInformation("Phase 5統合版PluginService のDispose処理が完了しました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Phase 5統合版PluginService のDispose処理中にエラーが発生しました");
            }
        }
    }

    /// <summary>
    /// Phase 5統合版：プラグイン情報実装クラス
    /// </summary>
    public class PluginInfo : IPluginInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime LoadedAt { get; set; } = DateTime.Now;
        public PluginStatus Status { get; set; } = PluginStatus.NotLoaded;
    }

    /// <summary>
    /// Phase 5統合版：プラグインコマンド情報実装クラス
    /// </summary>
    public class PluginCommandInfo : IPluginCommandInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string PluginId { get; set; } = string.Empty;
        public Type CommandType { get; set; } = typeof(object);
        public Type? SettingsType { get; set; }
        public string? IconPath { get; set; }
    }
}