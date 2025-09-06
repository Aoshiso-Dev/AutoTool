using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Messaging;

namespace AutoTool.Services
{
    /// <summary>
    /// 変数ストアのインターフェース（AutoTool版）
    /// </summary>
    public interface IVariableStoreService
    {
        /// <summary>
        /// 変数を設定
        /// </summary>
        void Set(string name, string value);

        /// <summary>
        /// 変数を取得
        /// </summary>
        string? Get(string name);

        /// <summary>
        /// 全ての変数をクリア
        /// </summary>
        void Clear();

        /// <summary>
        /// 全ての変数を取得
        /// </summary>
        Dictionary<string, string> GetAll();

        /// <summary>
        /// 変数が存在するかチェック
        /// </summary>
        bool Contains(string name);

        /// <summary>
        /// 変数を削除
        /// </summary>
        bool Remove(string name);

        /// <summary>
        /// 変数の数を取得
        /// </summary>
        int Count { get; }
    }

    /// <summary>
    /// 変数ストアの実装（AutoTool版・DI対応）
    /// </summary>
    public class VariableStoreService : IVariableStoreService
    {
        private readonly ConcurrentDictionary<string, string> _vars = new(StringComparer.OrdinalIgnoreCase);
        private readonly ILogger<VariableStoreService>? _logger;
        private readonly IMessenger? _messenger;

        public int Count => _vars.Count;

        public VariableStoreService(ILogger<VariableStoreService>? logger = null, IMessenger? messenger = null)
        {
            _logger = logger;
            _messenger = messenger;
            _logger?.LogDebug("VariableStore初期化完了");
        }

        public void Set(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger?.LogWarning("変数名が空またはnullです");
                return;
            }

            var oldValue = _vars.TryGetValue(name, out var existing) ? existing : null;
            _vars[name] = value ?? string.Empty;
            
            _logger?.LogDebug("変数設定: {Name} = {Value} (旧値: {OldValue})", name, value, oldValue ?? "なし");
            
            // TODO: 変数変更通知（将来実装）
            // _messenger?.Send(new VariableChangedMessage(name, value, oldValue));
        }

        public string? Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger?.LogWarning("変数名が空またはnullです");
                return null;
            }

            var result = _vars.TryGetValue(name, out var v) ? v : null;
            _logger?.LogTrace("変数取得: {Name} = {Value}", name, result ?? "null");
            return result;
        }

        public void Clear()
        {
            var count = _vars.Count;
            _vars.Clear();
            _logger?.LogInformation("変数ストアをクリアしました: {Count}件削除", count);
            
            // TODO: 変数全クリア通知（将来実装）
            // _messenger?.Send(new VariablesClearedMessage());
        }

        public Dictionary<string, string> GetAll()
        {
            return new Dictionary<string, string>(_vars, StringComparer.OrdinalIgnoreCase);
        }

        public bool Contains(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return _vars.ContainsKey(name);
        }

        public bool Remove(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            if (_vars.TryRemove(name, out var removedValue))
            {
                _logger?.LogDebug("変数削除: {Name} = {Value}", name, removedValue);
                
                // TODO: 変数削除通知（将来実装）
                // _messenger?.Send(new VariableRemovedMessage(name, removedValue));
                return true;
            }

            return false;
        }

        /// <summary>
        /// デバッグ用：全変数の状態をログ出力
        /// </summary>
        public void LogAllVariables()
        {
            if (_vars.IsEmpty)
            {
                _logger?.LogInformation("変数ストア: 変数は設定されていません");
                return;
            }

            _logger?.LogInformation("変数ストア状態: {Count}件の変数", _vars.Count);
            foreach (var kvp in _vars)
            {
                _logger?.LogInformation("  {Name} = {Value}", kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// 型安全な変数取得（数値）
        /// </summary>
        public int GetInt(string name, int defaultValue = 0)
        {
            var value = Get(name);
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// 型安全な変数取得（真偽値）
        /// </summary>
        public bool GetBool(string name, bool defaultValue = false)
        {
            var value = Get(name);
            return bool.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// 型安全な変数取得（小数）
        /// </summary>
        public double GetDouble(string name, double defaultValue = 0.0)
        {
            var value = Get(name);
            return double.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// 型安全な変数設定（数値）
        /// </summary>
        public void SetInt(string name, int value)
        {
            Set(name, value.ToString());
        }

        /// <summary>
        /// 型安全な変数設定（真偽値）
        /// </summary>
        public void SetBool(string name, bool value)
        {
            Set(name, value.ToString());
        }

        /// <summary>
        /// 型安全な変数設定（小数）
        /// </summary>
        public void SetDouble(string name, double value)
        {
            Set(name, value.ToString());
        }
    }
}