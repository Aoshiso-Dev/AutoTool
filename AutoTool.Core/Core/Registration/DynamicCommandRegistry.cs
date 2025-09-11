using System;
using System.Collections.Generic;
using System.Linq;
using AutoTool.Core.Descriptors;
using AutoTool.Core.Services;
using Microsoft.Extensions.Logging;

namespace AutoTool.Core.Registration
{
    /// <summary>
    /// 動的なコマンド登録をサポートするCommandRegistry
    /// </summary>
    public sealed class DynamicCommandRegistry : ICommandRegistry, IDynamicCommandRegistry
    {
        private readonly Dictionary<string, ICommandDescriptor> _map = new(StringComparer.Ordinal);
        private readonly ILogger<DynamicCommandRegistry>? _logger;
        private readonly object _lock = new object();

        public DynamicCommandRegistry(IEnumerable<ICommandDescriptor>? initialDescriptors = null, ILogger<DynamicCommandRegistry>? logger = null)
        {
            _logger = logger;

            if (initialDescriptors != null)
            {
                foreach (var descriptor in initialDescriptors)
                {
                    _map[descriptor.Type] = descriptor;
                }
                _logger?.LogDebug("DynamicCommandRegistry初期化: {Count}個の初期Descriptorを登録", _map.Count);
            }
        }

        public IEnumerable<ICommandDescriptor> All
        {
            get
            {
                lock (_lock)
                {
                    return _map.Values.ToArray(); // スナップショットを返す
                }
            }
        }

        public ICommandDescriptor Get(string type)
        {
            lock (_lock)
            {
                return _map.TryGetValue(type, out var d) ? d : throw new KeyNotFoundException($"Descriptor not found: {type}");
            }
        }

        public bool TryGet(string type, out ICommandDescriptor? descriptor)
        {
            lock (_lock)
            {
                return _map.TryGetValue(type, out descriptor);
            }
        }

        public void Register(ICommandDescriptor descriptor)
        {
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));

            lock (_lock)
            {
                var wasExisting = _map.ContainsKey(descriptor.Type);
                _map[descriptor.Type] = descriptor;
                
                var action = wasExisting ? "更新" : "登録";
                _logger?.LogDebug("コマンドDescriptor{Action}: Type={Type}, DisplayName={DisplayName}", 
                    action, descriptor.Type, descriptor.DisplayName);
            }
        }

        public bool Unregister(string type)
        {
            if (string.IsNullOrEmpty(type)) return false;

            lock (_lock)
            {
                var removed = _map.Remove(type);
                if (removed)
                {
                    _logger?.LogDebug("コマンドDescriptor削除: Type={Type}", type);
                }
                return removed;
            }
        }

        /// <summary>
        /// 複数のDescriptorを一括登録
        /// </summary>
        /// <param name="descriptors">登録するDescriptor配列</param>
        /// <returns>実際に登録された数</returns>
        public int RegisterMany(IEnumerable<ICommandDescriptor> descriptors)
        {
            if (descriptors == null) throw new ArgumentNullException(nameof(descriptors));

            var count = 0;
            foreach (var descriptor in descriptors)
            {
                Register(descriptor);
                count++;
            }

            _logger?.LogInformation("複数コマンドDescriptor一括登録完了: {Count}個", count);
            return count;
        }

        /// <summary>
        /// 指定されたタイプのコマンドをすべて削除
        /// </summary>
        /// <param name="types">削除するタイプ配列</param>
        /// <returns>実際に削除された数</returns>
        public int UnregisterMany(IEnumerable<string> types)
        {
            if (types == null) throw new ArgumentNullException(nameof(types));

            var count = 0;
            foreach (var type in types)
            {
                if (Unregister(type))
                {
                    count++;
                }
            }

            _logger?.LogInformation("複数コマンドDescriptor一括削除完了: {Count}個", count);
            return count;
        }

        /// <summary>
        /// レジストリをクリア
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                var count = _map.Count;
                _map.Clear();
                _logger?.LogInformation("コマンドRegistryクリア: {Count}個のDescriptorを削除", count);
            }
        }

        /// <summary>
        /// 現在の登録状況の統計を取得
        /// </summary>
        public RegistryStatistics GetStatistics()
        {
            lock (_lock)
            {
                var categoryGroups = _map.Values
                    .OfType<AttributeBasedDescriptor>()
                    .GroupBy(d => d.Category ?? "その他")
                    .ToDictionary(g => g.Key, g => g.Count());

                // 従来のDescriptorも含める
                var totalDescriptors = _map.Count;
                var attributeBasedCount = _map.Values.OfType<AttributeBasedDescriptor>().Count();
                var traditionalCount = totalDescriptors - attributeBasedCount;

                return new RegistryStatistics
                {
                    TotalDescriptors = totalDescriptors,
                    AttributeBasedCount = attributeBasedCount,
                    TraditionalCount = traditionalCount,
                    CategoryCounts = categoryGroups,
                    RegisteredTypes = _map.Keys.ToArray()
                };
            }
        }
    }

    /// <summary>
    /// レジストリの統計情報
    /// </summary>
    public sealed class RegistryStatistics
    {
        public int TotalDescriptors { get; set; }
        public int AttributeBasedCount { get; set; }
        public int TraditionalCount { get; set; }
        public Dictionary<string, int> CategoryCounts { get; set; } = new();
        public string[] RegisteredTypes { get; set; } = Array.Empty<string>();

        public override string ToString()
        {
            var categories = string.Join(", ", CategoryCounts.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
            return $"Total:{TotalDescriptors} (Attr:{AttributeBasedCount}, Trad:{TraditionalCount}) Categories:[{categories}]";
        }
    }
}