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
    /// Phase 5���S�����ŁF�v���O�C���T�[�r�X
    /// MacroPanels�ˑ����폜���AAutoTool�����ł̂ݎg�p
    /// </summary>
    public class PluginService : AutoTool.Services.Plugin.IPluginService
    {
        private readonly ILogger<PluginService> _logger;
        private readonly ConcurrentDictionary<string, IPlugin> _loadedPlugins;
        private readonly ConcurrentDictionary<string, IPluginCommandInfo> _availableCommands;

        // Phase 5�����ŃC�x���g
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
                _logger.LogInformation("Phase 5�����Ńv���O�C���ǂݍ��݊J�n: {PluginPath}", pluginPath);

                if (string.IsNullOrEmpty(pluginPath) || !File.Exists(pluginPath))
                {
                    throw new FileNotFoundException($"�v���O�C���t�@�C����������܂���: {pluginPath}");
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
                        
                        // �C�x���g����
                        PluginLoaded?.Invoke(this, new PluginLoadedEventArgs(plugin.Info));
                        
                        _logger.LogInformation("Phase 5�����Ńv���O�C���ǂݍ��݊���: {PluginId}", plugin.Info.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "�v���O�C���C���X�^���X�쐬�G���[: {PluginType}", pluginType.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Phase 5�����Ńv���O�C���ǂݍ��݃G���[: {PluginPath}", pluginPath);
                throw;
            }
        }

        public async Task LoadAllPluginsAsync()
        {
            try
            {
                _logger.LogInformation("Phase 5�����őS�v���O�C���ǂݍ��݊J�n");

                var pluginDirectory = Path.Combine(AppContext.BaseDirectory, "Plugins");
                if (!Directory.Exists(pluginDirectory))
                {
                    _logger.LogWarning("�v���O�C���f�B���N�g�������݂��܂���: {PluginDirectory}", pluginDirectory);
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
                        _logger.LogWarning(ex, "�v���O�C���t�@�C���ǂݍ��݃X�L�b�v: {PluginFile}", pluginFile);
                    }
                }

                _logger.LogInformation("Phase 5�����őS�v���O�C���ǂݍ��݊���: {Count}��", _loadedPlugins.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Phase 5�����őS�v���O�C���ǂݍ��݃G���[");
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
                    
                    // �v���O�C���֘A�̃R�}���h���폜
                    var commandsToRemove = _availableCommands
                        .Where(kvp => kvp.Value.PluginId == pluginId)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var commandId in commandsToRemove)
                    {
                        _availableCommands.TryRemove(commandId, out _);
                    }

                    // �C�x���g����
                    PluginUnloaded?.Invoke(this, new PluginUnloadedEventArgs(pluginId));
                    
                    _logger.LogInformation("Phase 5�����Ńv���O�C���A�����[�h����: {PluginId}", pluginId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Phase 5�����Ńv���O�C���A�����[�h�G���[: {PluginId}", pluginId);
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
                _logger.LogDebug("Phase 5�����Ńv���O�C���R�}���h�쐬: {PluginId}.{CommandId}", pluginId, commandId);

                if (_availableCommands.TryGetValue($"{pluginId}.{commandId}", out var commandInfo))
                {
                    var command = Activator.CreateInstance(commandInfo.CommandType);
                    _logger.LogDebug("Phase 5�����Ńv���O�C���R�}���h�쐬����: {CommandType}", commandInfo.CommandType.Name);
                    return command;
                }

                _logger.LogWarning("Phase 5�����Ńv���O�C���R�}���h��������܂���: {PluginId}.{CommandId}", pluginId, commandId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Phase 5�����Ńv���O�C���R�}���h�쐬�G���[: {PluginId}.{CommandId}", pluginId, commandId);
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
                _logger.LogInformation("Phase 5������PluginService ��Dispose�������J�n���܂�");

                var unloadTasks = _loadedPlugins.Keys.Select(pluginId => UnloadPluginAsync(pluginId));
                Task.WaitAll(unloadTasks.ToArray(), TimeSpan.FromSeconds(30));

                _loadedPlugins.Clear();
                _availableCommands.Clear();

                _logger.LogInformation("Phase 5������PluginService ��Dispose�������������܂���");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Phase 5������PluginService ��Dispose�������ɃG���[���������܂���");
            }
        }
    }

    /// <summary>
    /// Phase 5�����ŁF�v���O�C���������N���X
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
    /// Phase 5�����ŁF�v���O�C���R�}���h�������N���X
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