using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Configuration;

namespace AutoTool.Services.Configuration
{
    /// <summary>
    /// �A�v���P�[�V�����ݒ�Ǘ��̉��P��
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
    /// �������ꂽ�ݒ�Ǘ��T�[�r�X
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
                    
                    // �^�ϊ������s
                    return (T)Convert.ChangeType(value, typeof(T)) ?? defaultValue;
                }
                
                return defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "�ݒ�l�̎擾�Ɏ��s: {Key}, �f�t�H���g�l��Ԃ��܂�", key);
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
                _logger.LogDebug("�ݒ�l�X�V: {Key} = {Value}", key, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ݒ�l�̐ݒ�Ɏ��s: {Key}", key);
                throw;
            }
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings, _jsonOptions);
                File.WriteAllText(_configFilePath, json);
                _logger.LogInformation("�ݒ�t�@�C���ۑ�����: {Path}", _configFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ݒ�t�@�C���̕ۑ��Ɏ��s: {Path}", _configFilePath);
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
                
                _logger.LogInformation("�ݒ�t�@�C���ǂݍ��݊���: {Count}����", _settings.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ݒ�t�@�C���̓ǂݍ��݂Ɏ��s: {Path}", _configFilePath);
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
                _logger.LogInformation("�ݒ�����Z�b�g���܂���");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ݒ�̃��Z�b�g�Ɏ��s");
                throw;
            }
        }

        private void CreateDefaultSettings()
        {
            _settings.Clear();
            
            // �f�t�H���g�ݒ�l
            SetValue("App:Theme", "Light");
            SetValue("App:Language", "ja-JP");
            SetValue("App:AutoSave", true);
            SetValue("App:AutoSaveInterval", 300);
            
            SetValue("Macro:DefaultTimeout", 5000);
            SetValue("Macro:DefaultInterval", 100);
            
            SetValue("UI:GridSplitterPosition", 0.5);
            SetValue("UI:WindowWidth", 1200.0);
            SetValue("UI:WindowHeight", 800.0);
            
            _logger.LogInformation("�f�t�H���g�ݒ���쐬���܂���");
        }
    }

    /// <summary>
    /// �ݒ�̃J�e�S���ʃA�N�Z�T�[
    /// </summary>
    public static class ConfigurationKeys
    {
        public static class App
        {
            public const string Theme = "App:Theme";
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