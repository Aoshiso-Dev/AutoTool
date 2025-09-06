using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Configuration;

namespace AutoTool.Services.Configuration
{
    /// <summary>
    /// アプリケーション設定管理の改善版
    /// </summary>
    public interface IEnhancedConfigurationService
    {
        T GetValue<T>(string key, T defaultValue = default(T));
        void SetValue<T>(string key, T value);
        void Save();
        void Load();
        void Reset();
        
        event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;
    }

    /// <summary>
    /// 強化された設定管理サービス
    /// </summary>
    public class EnhancedConfigurationService : IEnhancedConfigurationService
    {
        private readonly ILogger<EnhancedConfigurationService> _logger;
        private readonly string _configFilePath;
        private readonly Dictionary<string, object> _settings;
        private readonly JsonSerializerOptions _jsonOptions;

        public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

        public EnhancedConfigurationService(ILogger<EnhancedConfigurationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configFilePath = Path.Combine(AppContext.BaseDirectory, "Settings.json");
            _settings = new Dictionary<string, object>();
            
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            Load();
        }

        public T GetValue<T>(string key, T defaultValue = default(T))
        {
            try
            {
                if (_settings.TryGetValue(key, out var value))
                {
                    if (value is JsonElement jsonElement)
                    {
                        return jsonElement.Deserialize<T>(_jsonOptions) ?? defaultValue;
                    }
                    
                    if (value is T directValue)
                    {
                        return directValue;
                    }
                    
                    // 型変換を試行
                    return (T)Convert.ChangeType(value, typeof(T)) ?? defaultValue;
                }
                
                return defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "設定値の取得に失敗: {Key}, デフォルト値を返します", key);
                return defaultValue;
            }
        }

        public void SetValue<T>(string key, T value)
        {
            try
            {
                var oldValue = _settings.TryGetValue(key, out var old) ? old : null;
                _settings[key] = value ?? throw new ArgumentNullException(nameof(value));
                
                ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs(key, oldValue, value));
                _logger.LogDebug("設定値更新: {Key} = {Value}", key, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定値の設定に失敗: {Key}", key);
                throw;
            }
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings, _jsonOptions);
                File.WriteAllText(_configFilePath, json);
                _logger.LogInformation("設定ファイル保存完了: {Path}", _configFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定ファイルの保存に失敗: {Path}", _configFilePath);
                throw;
            }
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    CreateDefaultSettings();
                    return;
                }

                var json = File.ReadAllText(_configFilePath);
                var loadedSettings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, _jsonOptions);
                
                if (loadedSettings != null)
                {
                    _settings.Clear();
                    foreach (var kvp in loadedSettings)
                    {
                        _settings[kvp.Key] = kvp.Value;
                    }
                }
                
                _logger.LogInformation("設定ファイル読み込み完了: {Count}項目", _settings.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定ファイルの読み込みに失敗: {Path}", _configFilePath);
                CreateDefaultSettings();
            }
        }

        public void Reset()
        {
            try
            {
                _settings.Clear();
                CreateDefaultSettings();
                Save();
                _logger.LogInformation("設定をリセットしました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定のリセットに失敗");
                throw;
            }
        }

        private void CreateDefaultSettings()
        {
            _settings.Clear();
            
            // デフォルト設定値
            SetValue("App:Language", "ja-JP");
            SetValue("App:AutoSave", true);
            SetValue("App:AutoSaveInterval", 300);
            
            SetValue("Macro:DefaultTimeout", 5000);
            SetValue("Macro:DefaultInterval", 100);
            
            SetValue("UI:GridSplitterPosition", 0.5);
            SetValue("UI:WindowWidth", 1200.0);
            SetValue("UI:WindowHeight", 800.0);
            
            _logger.LogInformation("デフォルト設定を作成しました");
        }
    }

    /// <summary>
    /// 設定のカテゴリ別アクセサー
    /// </summary>
    public static class ConfigurationKeys
    {
        public static class App
        {
            public const string Language = "App:Language";
            public const string AutoSave = "App:AutoSave";
            public const string AutoSaveInterval = "App:AutoSaveInterval";
        }

        public static class Macro
        {
            public const string DefaultTimeout = "Macro:DefaultTimeout";
            public const string DefaultInterval = "Macro:DefaultInterval";
        }

        public static class UI
        {
            public const string GridSplitterPosition = "UI:GridSplitterPosition";
            public const string WindowWidth = "UI:WindowWidth";
            public const string WindowHeight = "UI:WindowHeight";
            public const string WindowState = "UI:WindowState";
        }
    }
}