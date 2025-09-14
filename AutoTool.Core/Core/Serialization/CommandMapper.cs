using System.Text.Json;
using AutoTool.Core.Abstractions;
using AutoTool.Core.Commands;
using AutoTool.Core.Descriptors;
using System.Collections.Generic;
using System.Linq;

namespace AutoTool.Core.Serialization
{
    public sealed class CommandMapper
    {
        private readonly ICommandRegistry _registry;
        private readonly JsonSerializerOptions _json;

        public CommandMapper(ICommandRegistry registry, JsonSerializerOptions? json = null)
        {
            _registry = registry;
            _json = json ?? JsonOptions.Create();
        }

        public CommandDto ToDto(IAutoToolCommand node)
        {
            var desc = _registry.Get(node.Type);

            // Settings を抽出
            object? settingsObj = (node.GetType().GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHasSettings<>))
                ?.GetProperty("Settings")?.GetValue(node));

            var elem = settingsObj is null
                ? JsonDocument.Parse("{}").RootElement
                : JsonSerializer.SerializeToElement(settingsObj, desc.SettingsType, _json);

            var dto = new CommandDto
            {
                Id = node.Id,
                Type = node.Type,
                IsEnabled = node.IsEnabled,
                Settings = elem
            };

            if (node is IHasBlocks hb)
            {
                dto.Blocks = hb.Blocks.ToDictionary(
                    b => b.Name,
                    b => b.Children.Select(ToDto).ToList(),
                    StringComparer.Ordinal);
            }

            return dto;
        }

        public IAutoToolCommand FromDto(CommandDto dto, IServiceProvider services)
        {
            var desc = _registry.Get(dto.Type);

            // Settings デシリアライズ → マイグレーション → 検証
            var settings = (AutoToolCommandSettings)JsonSerializer.Deserialize(dto.Settings, desc.SettingsType, _json)!;
            settings = desc.MigrateToLatest(settings);
            var errors = desc.ValidateSettings(settings).ToArray();
            if (errors.Length > 0)
                throw new InvalidOperationException($"Invalid settings for {dto.Type}: {string.Join("; ", errors)}");

            var cmd = desc.CreateCommand(settings, services);
            cmd.IsEnabled = dto.IsEnabled;

            // 柔軟化：まず DTO に入っているブロック定義を元に復元する（BlockSlots に依存しない）
            if (cmd is IHasBlocks hb && dto.Blocks is { Count: > 0 })
            {
                // dto.Blocks に含まれる各ブロック名について、コマンド側の Blocks を名称で探索して復元する
                foreach (var kv in dto.Blocks)
                {
                    var blockName = kv.Key;
                    var childDtos = kv.Value;

                    var block = hb.Blocks.FirstOrDefault(b => string.Equals(b.Name, blockName, StringComparison.Ordinal));
                    if (block is null) continue;

                    block.Children.Clear();
                    foreach (var childDto in childDtos)
                    {
                        block.Children.Add(FromDto(childDto, services));
                    }
                }

                // 続いて BlockSlots が定義されているならその制約を検証する（AllowEmpty/Min/Max）
                if (desc.BlockSlots != null && desc.BlockSlots.Count > 0)
                {
                    foreach (var slot in desc.BlockSlots)
                    {
                        var block = hb.Blocks.FirstOrDefault(b => string.Equals(b.Name, slot.Name, StringComparison.Ordinal));
                        if (block is null) continue;

                        if (!slot.AllowEmpty && block.Children.Count == 0)
                            throw new InvalidOperationException($"Block '{slot.Name}' must not be empty in '{dto.Type}'.");

                        if (block.Children.Count < slot.Min)
                            throw new InvalidOperationException($"Block '{slot.Name}' requires at least {slot.Min} children in '{dto.Type}'.");

                        if (slot.Max is int max && block.Children.Count > max)
                            throw new InvalidOperationException($"Block '{slot.Name}' allows at most {max} children in '{dto.Type}'.");
                    }
                }
            }

            return cmd;
        }
    }
}