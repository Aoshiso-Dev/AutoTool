using AutoTool.Core.Abstractions;
using AutoTool.Core.Commands;
using AutoTool.Core.Descriptors;
using System;

namespace AutoTool.Commands.Flow.Wait
{
    /// <summary>
    /// Waitコマンドのディスクリプタ
    /// </summary>
    public sealed class WaitDescriptor : ICommandDescriptor
    {
        public string Type => "wait";
        public string DisplayName => "Wait";
        public string? IconKey => "mdi:timer";

        public Type SettingsType => typeof(WaitSettings);
        public int LatestSettingsVersion => 1;

        public IReadOnlyList<BlockSlot> BlockSlots { get; } = Array.Empty<BlockSlot>();

        public AutoToolCommandSettings CreateDefaultSettings() => new WaitSettings();

        public AutoToolCommandSettings MigrateToLatest(AutoToolCommandSettings settings)
        {
            if (settings is WaitSettings s)
                return s with { Version = LatestSettingsVersion };
            throw new InvalidCastException($"Unexpected settings type: {settings.GetType().Name}");
        }

        public IEnumerable<string> ValidateSettings(AutoToolCommandSettings settings)
        {
            if (settings is not WaitSettings s) { yield return "Settings type mismatch."; yield break; }
            if (s.DurationMs < 0) yield return "DurationMs must be non-negative.";
            if (s.DurationMs > 300000) yield return "DurationMs exceeds 5 minutes. Please check if this is intended.";
        }

        public IAutoToolCommand CreateCommand(AutoToolCommandSettings settings, IServiceProvider _)
            => new WaitCommand((WaitSettings)settings);
    }
}