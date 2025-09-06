using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AutoTool.Services.Configuration
{
    /// <summary>
    /// JSON ベースの設定管理サービス実装
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
            _logger.LogInformation("JsonConfigurationService を初期化しました: {ConfigPath}", _configFilePath);
        }

        /// <summary>
        /// 設定値を取得
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

                        // 型変換を試行
                        return (T)Convert.ChangeType(value, typeof(T));
                    }

                    _logger.LogDebug("設定キーが見つからないためデフォルト値を返します: {Key} = {DefaultValue}", key, defaultValue);
                    return defaultValue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "設定値の取得中にエラーが発生しました: {Key}", key);
                    return defaultValue;
                }
            }
        }

        /// <summary>
        /// 設定値を設定
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

                    _logger.LogDebug("設定値を更新しました: {Key} = {Value}", key, value);

                    // 変更イベントを発火
                    ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs(key, oldValue, value));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "設定値の設定中にエラーが発生しました: {Key}", key);
                    throw;
                }
            }
        }

        /// <summary>
        /// 設定をファイルに保存
        /// </summary>
        public async Task SaveAsync()
        {
            try
            {
                _logger.LogDebug("設定をファイルに保存します: {ConfigPath}", _configFilePath);

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

                _logger.LogInformation("設定ファイル保存完了: {Count}項目", configToSave.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定ファイルの保存に失敗しました");
                throw;
            }
        }

        /// <summary>
        /// 設定をファイルから読み込み
        /// </summary>
        public async Task LoadAsync()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    _logger.LogInformation("設定ファイルが存在しないため、デフォルト設定でスタートします");
                    await CreateDefaultConfiguration();
                    return;
                }

                _logger.LogDebug("設定ファイルを読み込みます: {ConfigPath}", _configFilePath);

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

                    _logger.LogInformation("設定ファイル読み込み完了: {Count}項目", loadedConfig.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定ファイルの読み込みに失敗しました");
                await CreateDefaultConfiguration();
            }
        }

        /// <summary>
        /// デフォルト設定を作成
        /// </summary>
        private async Task CreateDefaultConfiguration()
        {
            try
            {
                _logger.LogInformation("デフォルト設定を作成します");

                // デフォルト設定値
                SetValue("App.Language", "ja-JP");
                SetValue("App.AutoSave", true);
                SetValue("App.AutoSaveInterval", 300); // 5分
                SetValue("Logging.Level", "Information");
                SetValue("Macro.DefaultTimeout", 5000);
                SetValue("Macro.DefaultInterval", 100);
                SetValue("UI.GridSplitterPosition", 0.5);
                SetValue("UI.TabIndex.List", 0);
                SetValue("UI.TabIndex.Edit", 0);

                await SaveAsync();
                _logger.LogInformation("デフォルト設定を作成しました");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "デフォルト設定の作成に失敗しました");
            }
        }

        /// <summary>
        /// ディレクトリが存在することを確認
        /// </summary>
        private void EnsureDirectoryExists()
        {
            var directory = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogDebug("設定ディレクトリを作成しました: {Directory}", directory);
            }
        }

        /// <summary>
        /// JsonElement を指定された型にデシリアライズ
        /// </summary>
        private T DeserializeJsonElement<T>(JsonElement element)
        {
            try
            {
                return element.Deserialize<T>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "JsonElement のデシリアライズに失敗しました: {Type}", typeof(T));
                return default(T);
            }
        }

        /// <summary>
        /// すべての設定を取得（デバッグ用）
        /// </summary>
        public Dictionary<string, object> GetAllSettings()
        {
            lock (_lock)
            {
                return new Dictionary<string, object>(_configuration);
            }
        }

        /// <summary>
        /// 設定をクリア
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _configuration.Clear();
                _logger.LogInformation("すべての設定をクリアしました");
            }
        }
    }
}