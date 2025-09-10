using AutoTool.Core.Abstractions;
using AutoTool.Core.Commands;
using AutoTool.Core.Descriptors;


namespace AutoTool.Commands.Flow.If;


public sealed class IfDescriptor : ICommandDescriptor
{
    public string Type => "if";
    public string DisplayName => "If";
    public string? IconKey => "mdi:code-braces";


    public Type SettingsType => typeof(IfSettings);
    public int LatestSettingsVersion => 1;


    public IReadOnlyList<BlockSlot> BlockSlots { get; } = new[]
    {
        new BlockSlot("Then", AllowEmpty: true),
        new BlockSlot("Else", AllowEmpty: true)
    };


    public AutoToolCommandSettings CreateDefaultSettings() => new IfSettings();

    public AutoToolCommandSettings MigrateToLatest(AutoToolCommandSettings settings)
    {
        if (settings is IfSettings s)
            return s with { Version = LatestSettingsVersion };
        throw new InvalidCastException($"Unexpected settings type: {settings.GetType().Name}");
    }

    public IEnumerable<string> ValidateSettings(AutoToolCommandSettings settings)
    {
        if (settings is not IfSettings s) { yield return "Settings type mismatch."; yield break; }
        if (string.IsNullOrWhiteSpace(s.ConditionExpr)) yield return "ConditionExpr is required.";
    }

    public IAutoToolCommand CreateCommand(AutoToolCommandSettings settings, IServiceProvider _)
        => new IfCommand((IfSettings)settings);
}