using AutoTool.Core.Abstractions;
using AutoTool.Core.Commands;
using AutoTool.Core.Descriptors;


namespace AutoTool.Commands.Flow.While;


public sealed class WhileDescriptor : ICommandDescriptor
{
    public string Type => "while";
    public string DisplayName => "繰り返し";
    public string? IconKey => "mdi:loop";


    public Type SettingsType => typeof(WhileSettings);
    public int LatestSettingsVersion => 1;


    public IReadOnlyList<BlockSlot> BlockSlots { get; } = new[]
    {
new BlockSlot("Body", AllowEmpty: true)
};


    public AutoToolCommandSettings CreateDefaultSettings() => new WhileSettings();


    public AutoToolCommandSettings MigrateToLatest(AutoToolCommandSettings settings)
    {
        if (settings is WhileSettings s)
        {
            return s with { Version = LatestSettingsVersion };
        }
        throw new InvalidCastException($"Unexpected settings type: {settings.GetType().Name}");
    }


    public IEnumerable<string> ValidateSettings(AutoToolCommandSettings settings)
    {
        if (settings is not WhileSettings s)
        {
            yield return "Settings type mismatch.";
            yield break;
        }
        if (string.IsNullOrWhiteSpace(s.ConditionExpr))
            yield return "ConditionExpr is required.";
        if (s.MaxIterations <= 0)
            yield return "MaxIterations must be > 0.";
    }


    public IAutoToolCommand CreateCommand(AutoToolCommandSettings settings, IServiceProvider _)
    => new WhileCommand((WhileSettings)settings);
}