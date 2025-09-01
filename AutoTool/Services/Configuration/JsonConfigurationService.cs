using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AutoTool.Services.Configuration
{
    /// <summary>
    /// JSON �x�[�X�̐ݒ�Ǘ��T�[�r�X����
    /// </summary>
    public class JsonConfigurationService : IConfigurationService
    {
        private readonly ILogger<JsonConfigurationService> _logger;
        private readonly string _configFilePath;
        private readonly Dictionary<string, object> _configuration;
        private readonly object _lock = new();

        public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

        public JsonConfigurationService(ILogger<JsonConfigurationService> logger)
        {
            _logger = logger;
            _configFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AutoTool", "appsettings.json");
            _configuration = new Dictionary<string, object>();
            
            EnsureDirectoryExists();
            _logger.LogInformation("JsonConfigurationService �����������܂���: {ConfigPath}", _configFilePath);
        }

        /// <summary>
        /// �ݒ�l���擾
        /// </summary>
        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            lock (_lock)
            {
                try
                {
                    if (_configuration.TryGetValue(key, out var value))
                    {
                        if (value is JsonElement jsonElement)
                        {
                            return DeserializeJsonElement<T>(jsonElement);
                        }
                        
                        if (value is T directValue)
                        {
                            return directValue;
                        }

                        // �^�ϊ������s
                        return (T)Convert.ChangeType(value, typeof(T));
                    }

                    _logger.LogDebug("�ݒ�L�[��������Ȃ����߃f�t�H���g�l��Ԃ��܂�: {Key} = {DefaultValue}", key, defaultValue);
                    return defaultValue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "�ݒ�l�̎擾���ɃG���[���������܂���: {Key}", key);
                    return defaultValue;
                }
            }
        }

        /// <summary>
        /// �ݒ�l��ݒ�
        /// </summary>
        public void SetValue<T>(string key, T value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            lock (_lock)
            {
                try
                {
                    var oldValue = _configuration.TryGetValue(key, out var old) ? old : null;
                    _configuration[key] = value;

                    _logger.LogDebug("�ݒ�l���X�V���܂���: {Key} = {Value}", key, value);

                    // �ύX�C�x���g�𔭉�
                    ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs(key, oldValue, value));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "�ݒ�l�̐ݒ蒆�ɃG���[���������܂���: {Key}", key);
                    throw;
                }
            }
        }

        /// <summary>
        /// �ݒ���t�@�C���ɕۑ�
        /// </summary>
        public async Task SaveAsync()
        {
            try
            {
                _logger.LogDebug("�ݒ���t�@�C���ɕۑ����܂�: {ConfigPath}", _configFilePath);

                Dictionary<string, object> configToSave;
                lock (_lock)
                {
                    configToSave = new Dictionary<string, object>(_configuration);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(configToSave, options);
                await File.WriteAllTextAsync(_configFilePath, json);

                _logger.LogInformation("�ݒ�t�@�C���ۑ�����: {Count}����", configToSave.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ݒ�t�@�C���̕ۑ��Ɏ��s���܂���");
                throw;
            }
        }

        /// <summary>
        /// �ݒ���t�@�C������ǂݍ���
        /// </summary>
        public async Task LoadAsync()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    _logger.LogInformation("�ݒ�t�@�C�������݂��Ȃ����߁A�f�t�H���g�ݒ�ŃX�^�[�g���܂�");
                    await CreateDefaultConfiguration();
                    return;
                }

                _logger.LogDebug("�ݒ�t�@�C����ǂݍ��݂܂�: {ConfigPath}", _configFilePath);

                var json = await File.ReadAllTextAsync(_configFilePath);
                var loadedConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                if (loadedConfig != null)
                {
                    lock (_lock)
                    {
                        _configuration.Clear();
                        foreach (var kvp in loadedConfig)
                        {
                            _configuration[kvp.Key] = kvp.Value;
                        }
                    }

                    _logger.LogInformation("�ݒ�t�@�C���ǂݍ��݊���: {Count}����", loadedConfig.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�ݒ�t�@�C���̓ǂݍ��݂Ɏ��s���܂���");
                await CreateDefaultConfiguration();
            }
        }

        /// <summary>
        /// �f�t�H���g�ݒ���쐬
        /// </summary>
        private async Task CreateDefaultConfiguration()
        {
            try
            {
                _logger.LogInformation("�f�t�H���g�ݒ���쐬���܂�");

                // �f�t�H���g�ݒ�l
                SetValue("App.Theme", "Light");
                SetValue("App.Language", "ja-JP");
                SetValue("App.AutoSave", true);
                SetValue("App.AutoSaveInterval", 300); // 5��
                SetValue("Logging.Level", "Information");
                SetValue("Macro.DefaultTimeout", 5000);
                SetValue("Macro.DefaultInterval", 100);
                SetValue("UI.GridSplitterPosition", 0.5);
                SetValue("UI.TabIndex.List", 0);
                SetValue("UI.TabIndex.Edit", 0);

                await SaveAsync();
                _logger.LogInformation("�f�t�H���g�ݒ���쐬���܂���");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�f�t�H���g�ݒ�̍쐬�Ɏ��s���܂���");
            }
        }

        /// <summary>
        /// �f�B���N�g�������݂��邱�Ƃ��m�F
        /// </summary>
        private void EnsureDirectoryExists()
        {
            var directory = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogDebug("�ݒ�f�B���N�g�����쐬���܂���: {Directory}", directory);
            }
        }

        /// <summary>
        /// JsonElement ���w�肳�ꂽ�^�Ƀf�V���A���C�Y
        /// </summary>
        private T DeserializeJsonElement<T>(JsonElement element)
        {
            try
            {
                return element.Deserialize<T>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "JsonElement �̃f�V���A���C�Y�Ɏ��s���܂���: {Type}", typeof(T));
                return default(T);
            }
        }

        /// <summary>
        /// ���ׂĂ̐ݒ���擾�i�f�o�b�O�p�j
        /// </summary>
        public Dictionary<string, object> GetAllSettings()
        {
            lock (_lock)
            {
                return new Dictionary<string, object>(_configuration);
            }
        }

        /// <summary>
        /// �ݒ���N���A
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _configuration.Clear();
                _logger.LogInformation("���ׂĂ̐ݒ���N���A���܂���");
            }
        }
    }
}