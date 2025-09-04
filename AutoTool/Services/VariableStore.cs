using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Messaging;

namespace AutoTool.Services
{
    /// <summary>
    /// �ϐ��X�g�A�̎����iAutoTool�ŁEDI�Ή��j
    /// </summary>
    public class VariableStore : IVariableStore
    {
        private readonly ConcurrentDictionary<string, string> _vars = new(StringComparer.OrdinalIgnoreCase);
        private readonly ILogger<VariableStore>? _logger;
        private readonly IMessenger? _messenger;

        public int Count => _vars.Count;

        public VariableStore(ILogger<VariableStore>? logger = null, IMessenger? messenger = null)
        {
            _logger = logger;
            _messenger = messenger;
            _logger?.LogDebug("VariableStore����������");
        }

        public void Set(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger?.LogWarning("�ϐ�������܂���null�ł�");
                return;
            }

            var oldValue = _vars.TryGetValue(name, out var existing) ? existing : null;
            _vars[name] = value ?? string.Empty;
            
            _logger?.LogDebug("�ϐ��ݒ�: {Name} = {Value} (���l: {OldValue})", name, value, oldValue ?? "�Ȃ�");
            
            // TODO: �ϐ��ύX�ʒm�i���������j
            // _messenger?.Send(new VariableChangedMessage(name, value, oldValue));
        }

        public string? Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger?.LogWarning("�ϐ�������܂���null�ł�");
                return null;
            }

            var result = _vars.TryGetValue(name, out var v) ? v : null;
            _logger?.LogTrace("�ϐ��擾: {Name} = {Value}", name, result ?? "null");
            return result;
        }

        public void Clear()
        {
            var count = _vars.Count;
            _vars.Clear();
            _logger?.LogInformation("�ϐ��X�g�A���N���A���܂���: {Count}���폜", count);
            
            // TODO: �ϐ��S�N���A�ʒm�i���������j
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
                _logger?.LogDebug("�ϐ��폜: {Name} = {Value}", name, removedValue);
                
                // TODO: �ϐ��폜�ʒm�i���������j
                // _messenger?.Send(new VariableRemovedMessage(name, removedValue));
                return true;
            }

            return false;
        }

        /// <summary>
        /// �f�o�b�O�p�F�S�ϐ��̏�Ԃ����O�o��
        /// </summary>
        public void LogAllVariables()
        {
            if (_vars.IsEmpty)
            {
                _logger?.LogInformation("�ϐ��X�g�A: �ϐ��͐ݒ肳��Ă��܂���");
                return;
            }

            _logger?.LogInformation("�ϐ��X�g�A���: {Count}���̕ϐ�", _vars.Count);
            foreach (var kvp in _vars)
            {
                _logger?.LogInformation("  {Name} = {Value}", kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// �^���S�ȕϐ��擾�i���l�j
        /// </summary>
        public int GetInt(string name, int defaultValue = 0)
        {
            var value = Get(name);
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// �^���S�ȕϐ��擾�i�^�U�l�j
        /// </summary>
        public bool GetBool(string name, bool defaultValue = false)
        {
            var value = Get(name);
            return bool.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// �^���S�ȕϐ��擾�i�����j
        /// </summary>
        public double GetDouble(string name, double defaultValue = 0.0)
        {
            var value = Get(name);
            return double.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// �^���S�ȕϐ��ݒ�i���l�j
        /// </summary>
        public void SetInt(string name, int value)
        {
            Set(name, value.ToString());
        }

        /// <summary>
        /// �^���S�ȕϐ��ݒ�i�^�U�l�j
        /// </summary>
        public void SetBool(string name, bool value)
        {
            Set(name, value.ToString());
        }

        /// <summary>
        /// �^���S�ȕϐ��ݒ�i�����j
        /// </summary>
        public void SetDouble(string name, double value)
        {
            Set(name, value.ToString());
        }
    }
}