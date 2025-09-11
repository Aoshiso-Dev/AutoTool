using AutoTool.Services.Abstractions;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.IO;

namespace AutoTool.Services.Implementations;

/// <summary>
/// 設定サービスの実装
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly Dictionary<string, object> _settings = new();
    private readonly string _configFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger;
        _configFilePath = Path.Combine(AppContext.BaseDirectory, "Settings.json");
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        LoadConfiguration();
    }

    public string ConfigFilePath => _configFilePath;

    public T? Get<T>(string key, T? defaultValue = default)
    {
        try
        {
            if (_settings.TryGetValue(key, out var value))
            {
                if (value is T directValue)
                    return directValue;

                if (value is JsonElement jsonElement)
                    return JsonSerializer.Deserialize<T>(jsonElement.GetRawText(), _jsonOptions);

                return (T)Convert.ChangeType(value, typeof(T));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get configuration value for key: {Key}", key);
        }

        return defaultValue;
    }

    public void Set<T>(string key, T value)
    {
        _settings[key] = value!;
        _logger.LogDebug("Configuration value set: {Key} = {Value}", key, value);
    }

    public bool ContainsKey(string key)
    {
        return _settings.ContainsKey(key);
    }

    public void Remove(string key)
    {
        if (_settings.Remove(key))
        {
            _logger.LogDebug("Configuration value removed: {Key}", key);
        }
    }

    public void Clear()
    {
        _settings.Clear();
        _logger.LogDebug("All configuration values cleared");
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var directory = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_settings, _jsonOptions);
            await File.WriteAllTextAsync(_configFilePath, json, cancellationToken);
            
            _logger.LogDebug("Configuration saved to: {FilePath}", _configFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save configuration to: {FilePath}", _configFilePath);
            throw;
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() => LoadConfiguration(), cancellationToken);
    }

    // 後方互換性のために保持
    private async Task SaveAsync()
    {
        await SaveAsync(CancellationToken.None);
    }

    private void LoadConfiguration()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                _logger.LogInformation("Configuration file not found, using default settings: {FilePath}", _configFilePath);
                return;
            }

            var json = File.ReadAllText(_configFilePath);
            var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions);
            
            if (settings != null)
            {
                _settings.Clear();
                foreach (var kvp in settings)
                {
                    _settings[kvp.Key] = kvp.Value;
                }
            }

            _logger.LogDebug("Configuration loaded from: {FilePath}", _configFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration from: {FilePath}", _configFilePath);
        }
    }
}