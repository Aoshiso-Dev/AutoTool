using System.Reflection;
using AutoTool.Core.Commands;
using AutoTool.Services.Abstractions;
using Microsoft.Extensions.Logging;
using System.IO;

namespace AutoTool.Services.Implementations;

/// <summary>
/// プラグインサービスの実装
/// </summary>
public class PluginService : IPluginService
{
    private readonly ILogger<PluginService> _logger;
    private readonly Dictionary<string, PluginInfo> _loadedPlugins = new();
    private readonly Dictionary<string, Type> _commandTypes = new();

    public PluginService(ILogger<PluginService> logger)
    {
        _logger = logger;
    }

    public async Task LoadPluginsAsync(string pluginDirectory, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(pluginDirectory))
        {
            _logger.LogWarning("Plugin directory not found: {Directory}", pluginDirectory);
            return;
        }

        _logger.LogInformation("Loading plugins from directory: {Directory}", pluginDirectory);

        var pluginFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly);
        
        foreach (var pluginFile in pluginFiles)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
                
            await LoadPluginAsync(pluginFile, cancellationToken);
        }
    }

    public async Task LoadPluginAsync(string pluginFilePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(pluginFilePath))
            {
                _logger.LogWarning("Plugin file not found: {FilePath}", pluginFilePath);
                return;
            }

            _logger.LogDebug("Loading plugin: {FilePath}", pluginFilePath);

            var assembly = Assembly.LoadFrom(pluginFilePath);
            var commandTypes = await Task.Run(() => 
                assembly.GetTypes()
                    .Where(t => typeof(IAutoToolCommand).IsAssignableFrom(t) && !t.IsAbstract)
                    .ToArray(), cancellationToken);

            if (commandTypes.Length == 0)
            {
                _logger.LogWarning("No command types found in plugin: {FilePath}", pluginFilePath);
                return;
            }

            var pluginName = Path.GetFileNameWithoutExtension(pluginFilePath);
            var pluginInfo = new PluginInfo
            {
                Name = pluginName,
                Version = assembly.GetName().Version?.ToString() ?? "Unknown",
                Description = GetAssemblyDescription(assembly),
                Author = GetAssemblyAuthor(assembly),
                FilePath = pluginFilePath,
                LoadedAt = DateTime.Now,
                CommandTypes = commandTypes.Select(t => t.Name)
            };

            _loadedPlugins[pluginName] = pluginInfo;

            foreach (var commandType in commandTypes)
            {
                _commandTypes[commandType.Name] = commandType;
                _logger.LogDebug("Registered command type: {TypeName}", commandType.Name);
            }

            _logger.LogInformation("Plugin loaded successfully: {PluginName} ({CommandCount} commands)", 
                pluginName, commandTypes.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin: {FilePath}", pluginFilePath);
        }
    }

    public IEnumerable<PluginInfo> GetLoadedPlugins()
    {
        return _loadedPlugins.Values.ToArray();
    }

    public IAutoToolCommand? CreateCommand(string commandType)
    {
        if (_commandTypes.TryGetValue(commandType, out var type))
        {
            try
            {
                var command = Activator.CreateInstance(type) as IAutoToolCommand;
                _logger.LogDebug("Created command instance: {CommandType}", commandType);
                return command;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create command instance: {CommandType}", commandType);
            }
        }
        else
        {
            _logger.LogWarning("Command type not found: {CommandType}", commandType);
        }

        return null;
    }

    public IEnumerable<string> GetAvailableCommandTypes()
    {
        return _commandTypes.Keys.ToArray();
    }

    public void UnloadPlugin(string pluginName)
    {
        if (_loadedPlugins.TryGetValue(pluginName, out var pluginInfo))
        {
            // Remove associated command types
            foreach (var commandTypeName in pluginInfo.CommandTypes)
            {
                _commandTypes.Remove(commandTypeName);
            }

            _loadedPlugins.Remove(pluginName);
            _logger.LogInformation("Plugin unloaded: {PluginName}", pluginName);
        }
    }

    public void UnloadAllPlugins()
    {
        var pluginNames = _loadedPlugins.Keys.ToArray();
        foreach (var pluginName in pluginNames)
        {
            UnloadPlugin(pluginName);
        }
        
        _logger.LogInformation("All plugins unloaded");
    }

    private string GetAssemblyDescription(Assembly assembly)
    {
        var attribute = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>();
        return attribute?.Description ?? "No description available";
    }

    private string GetAssemblyAuthor(Assembly assembly)
    {
        var attribute = assembly.GetCustomAttribute<AssemblyCompanyAttribute>();
        return attribute?.Company ?? "Unknown";
    }
}