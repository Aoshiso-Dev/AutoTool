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
    /// �v���O�C���T�[�r�X�̎���
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
            _logger.LogInformation("PluginService ����������: {PluginDirectory}", _pluginsDirectory);
        }

        /// <summary>
        /// �v���O�C����ǂݍ���
        /// </summary>
        public async Task LoadPluginAsync(string pluginPath)
        {
            if (string.IsNullOrWhiteSpace(pluginPath))
                throw new ArgumentException("Plugin path cannot be null or empty", nameof(pluginPath));

            if (!File.Exists(pluginPath))
                throw new FileNotFoundException($"Plugin file not found: {pluginPath}");

            try
            {
                _logger.LogInformation("�v���O�C����ǂݍ��ݒ�: {PluginPath}", pluginPath);

                // �A�Z���u���ǂݍ��݃R���e�L�X�g���쐬
                var loadContext = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(pluginPath), true);
                var assembly = loadContext.LoadFromAssemblyPath(pluginPath);

                // IPlugin�����^������
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(MacroPanels.Plugin.IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    .ToList();

                if (!pluginTypes.Any())
                {
                    _logger.LogWarning("�v���O�C���A�Z���u����IPlugin�����^��������܂���: {PluginPath}", pluginPath);
                    return;
                }

                foreach (var pluginType in pluginTypes)
                {
                    await LoadPluginInstance(pluginType, loadContext, pluginPath);
                }

                _logger.LogInformation("�v���O�C���ǂݍ��݊���: {PluginPath}, �ǂݍ��ݍς݌^��: {TypeCount}", 
                    pluginPath, pluginTypes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�v���O�C���ǂݍ��ݒ��ɃG���[������: {PluginPath}", pluginPath);
                throw;
            }
        }

        /// <summary>
        /// �S�Ẵv���O�C����ǂݍ���
        /// </summary>
        public async Task LoadAllPluginsAsync()
        {
            try
            {
                _logger.LogInformation("�S�v���O�C���̓ǂݍ��݂��J�n");

                if (!Directory.Exists(_pluginsDirectory))
                {
                    _logger.LogInformation("�v���O�C���f�B���N�g�������݂��܂���: {Directory}", _pluginsDirectory);
                    return;
                }

                var pluginFiles = Directory.GetFiles(_pluginsDirectory, "*.dll", SearchOption.AllDirectories);
                _logger.LogInformation("�v���O�C���t�@�C������: {Count}��", pluginFiles.Length);

                var loadTasks = pluginFiles.Select(LoadPluginAsync);
                await Task.WhenAll(loadTasks);

                _logger.LogInformation("�S�v���O�C���̓ǂݍ��݊���: {LoadedCount}��", _loadedPlugins.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�S�v���O�C���ǂݍ��ݒ��ɃG���[���������܂���");
                throw;
            }
        }

        /// <summary>
        /// �v���O�C�����A�����[�h
        /// </summary>
        public async Task UnloadPluginAsync(string pluginId)
        {
            if (string.IsNullOrWhiteSpace(pluginId))
                throw new ArgumentException("Plugin ID cannot be null or empty", nameof(pluginId));

            try
            {
                if (_loadedPlugins.TryRemove(pluginId, out var loadedPlugin))
                {
                    _logger.LogInformation("�v���O�C�����A�����[�h��: {PluginId}", pluginId);

                    loadedPlugin.Info.Status = MacroPanels.Plugin.PluginStatus.Unloading;

                    // �R�}���h�v���O�C���̏ꍇ�A�R�}���h�����폜
                    if (loadedPlugin.Instance is MacroPanels.Plugin.ICommandPlugin commandPlugin)
                    {
                        var commandsToRemove = _availableCommands.Where(kvp => kvp.Value.PluginId == pluginId)
                            .Select(kvp => kvp.Key)
                            .ToList();

                        foreach (var commandId in commandsToRemove)
                        {
                            _availableCommands.TryRemove(commandId, out _);
                        }

                        _logger.LogDebug("�v���O�C���R�}���h���폜: {CommandCount}��", commandsToRemove.Count);
                    }

                    // �v���O�C���̃V���b�g�_�E������
                    try
                    {
                        await loadedPlugin.Instance.ShutdownAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "�v���O�C���V���b�g�_�E�����ɃG���[: {PluginId}", pluginId);
                    }

                    // �A�Z���u���ǂݍ��݃R���e�L�X�g���A�����[�h
                    loadedPlugin.LoadContext?.Unload();

                    PluginUnloaded?.Invoke(this, new MacroPanels.Plugin.PluginUnloadedEventArgs(pluginId));
                    _logger.LogInformation("�v���O�C���A�����[�h����: {PluginId}", pluginId);
                }
                else
                {
                    _logger.LogWarning("�A�����[�h�Ώۂ̃v���O�C����������܂���: {PluginId}", pluginId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�v���O�C���A�����[�h���ɃG���[: {PluginId}", pluginId);
                throw;
            }
        }

        /// <summary>
        /// �ǂݍ��ݍς݃v���O�C���ꗗ���擾
        /// </summary>
        public IEnumerable<MacroPanels.Plugin.IPluginInfo> GetLoadedPlugins()
        {
            return _loadedPlugins.Values.Select(p => p.Info).ToList();
        }

        /// <summary>
        /// �v���O�C�����擾
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
        /// �R�}���h�v���O�C������R�}���h���쐬
        /// </summary>
        public ICommand? CreatePluginCommand(string pluginId, string commandId, ICommand? parent, object? settings)
        {
            try
            {
                var plugin = GetPlugin<MacroPanels.Plugin.ICommandPlugin>(pluginId);
                if (plugin == null)
                {
                    _logger.LogWarning("�R�}���h�v���O�C����������܂���: {PluginId}", pluginId);
                    return null;
                }

                if (!plugin.IsCommandAvailable(commandId))
                {
                    _logger.LogWarning("�R�}���h�����p�ł��܂���: {PluginId}.{CommandId}", pluginId, commandId);
                    return null;
                }

                var command = plugin.CreateCommand(commandId, parent, settings);
                _logger.LogDebug("�v���O�C���R�}���h�쐬����: {PluginId}.{CommandId}", pluginId, commandId);
                
                return command;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�v���O�C���R�}���h�쐬�G���[: {PluginId}.{CommandId}", pluginId, commandId);
                return null;
            }
        }

        /// <summary>
        /// ���p�\�ȃv���O�C���R�}���h���擾
        /// </summary>
        public IEnumerable<MacroPanels.Plugin.IPluginCommandInfo> GetAvailablePluginCommands()
        {
            return _availableCommands.Values.ToList();
        }

        /// <summary>
        /// �v���O�C���C���X�^���X��ǂݍ���
        /// </summary>
        private async Task LoadPluginInstance(Type pluginType, AssemblyLoadContext loadContext, string pluginPath)
        {
            try
            {
                _logger.LogDebug("�v���O�C���C���X�^���X���쐬: {PluginType}", pluginType.FullName);

                var pluginInstance = Activator.CreateInstance(pluginType) as MacroPanels.Plugin.IPlugin;
                if (pluginInstance == null)
                {
                    _logger.LogWarning("�v���O�C���C���X�^���X�̍쐬�Ɏ��s: {PluginType}", pluginType.FullName);
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

                // �v���O�C��������
                await pluginInstance.InitializeAsync();
                pluginInfo.Status = MacroPanels.Plugin.PluginStatus.Active;

                // �R�}���h�v���O�C���̏ꍇ�A�R�}���h����o�^
                if (pluginInstance is MacroPanels.Plugin.ICommandPlugin commandPlugin)
                {
                    RegisterCommandPluginCommands(commandPlugin);
                }

                _loadedPlugins[pluginInfo.Id] = loadedPlugin;

                PluginLoaded?.Invoke(this, new MacroPanels.Plugin.PluginLoadedEventArgs(pluginInfo));
                _logger.LogInformation("�v���O�C���C���X�^���X�쐬����: {PluginId} ({PluginName})", 
                    pluginInfo.Id, pluginInfo.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�v���O�C���C���X�^���X�쐬�G���[: {PluginType}", pluginType.FullName);
                throw;
            }
        }

        /// <summary>
        /// �R�}���h�v���O�C���̃R�}���h��o�^
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
                    
                    _logger.LogDebug("�v���O�C���R�}���h�o�^: {FullCommandId} ({Name})", 
                        fullCommandId, commandInfo.Name);
                }

                _logger.LogInformation("�R�}���h�v���O�C���o�^����: {PluginId}, �R�}���h��: {CommandCount}", 
                    commandPlugin.Info.Id, commands.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�R�}���h�v���O�C���o�^�G���[: {PluginId}", commandPlugin.Info.Id);
            }
        }

        /// <summary>
        /// �v���O�C���f�B���N�g�������݂��邱�Ƃ��m�F
        /// </summary>
        private void EnsurePluginDirectoryExists()
        {
            if (!Directory.Exists(_pluginsDirectory))
            {
                Directory.CreateDirectory(_pluginsDirectory);
                _logger.LogDebug("�v���O�C���f�B���N�g�����쐬: {Directory}", _pluginsDirectory);
            }
        }

        /// <summary>
        /// �ǂݍ��ݍς݃v���O�C�����
        /// </summary>
        private class LoadedPlugin
        {
            public MacroPanels.Plugin.IPlugin Instance { get; set; } = null!;
            public PluginInfo Info { get; set; } = null!;
            public AssemblyLoadContext? LoadContext { get; set; }
            public string AssemblyPath { get; set; } = string.Empty;
        }

        /// <summary>
        /// �v���O�C�����̎���
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