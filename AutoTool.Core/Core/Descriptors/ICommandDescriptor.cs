using AutoTool.Core.Abstractions;
using AutoTool.Core.Commands;
using AutoTool.Core.Descriptors;

public interface ICommandDescriptor
{
    string Type { get; }
    string DisplayName { get; }
    string? IconKey { get; }

    Type SettingsType { get; }                 // AutoToolCommandSettings の派生型
    int LatestSettingsVersion { get; }

    IReadOnlyList<BlockSlot> BlockSlots { get; }

    AutoToolCommandSettings CreateDefaultSettings();
    AutoToolCommandSettings MigrateToLatest(AutoToolCommandSettings settings);
    IEnumerable<string> ValidateSettings(AutoToolCommandSettings settings);

    IAutoToolCommand CreateCommand(AutoToolCommandSettings settings, IServiceProvider services);
}