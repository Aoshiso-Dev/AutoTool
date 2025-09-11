using AutoTool.Core.Abstractions;
using AutoTool.Core.Commands;
using AutoTool.Core.Descriptors;

namespace AutoTool.Commands.Input.Click
{
    /// <summary>
    /// Clickコマンドのディスクリプタ
    /// </summary>
    public sealed class ClickDescriptor : ICommandDescriptor
    {
        public string Type => "click";
        public string DisplayName => "クリック";
        public string? IconKey => "mdi:cursor-default-click";

        public Type SettingsType => typeof(ClickSettings);
        public int LatestSettingsVersion => 1;

        public IReadOnlyList<BlockSlot> BlockSlots { get; } = Array.Empty<BlockSlot>();

        public AutoToolCommandSettings CreateDefaultSettings() => new ClickSettings
        {
            X = 100,
            Y = 100,
            Button = MouseButton.Left,
            Description = "指定座標をクリック"
        };

        public AutoToolCommandSettings MigrateToLatest(AutoToolCommandSettings settings)
        {
            if (settings is ClickSettings s)
                return s with { Version = LatestSettingsVersion };
            throw new InvalidCastException($"Unexpected settings type: {settings.GetType().Name}");
        }

        public IEnumerable<string> ValidateSettings(AutoToolCommandSettings settings)
        {
            if (settings is not ClickSettings s) { yield return "Settings type mismatch."; yield break; }
            
            if (s.X < 0) yield return "X coordinate must be non-negative.";
            if (s.Y < 0) yield return "Y coordinate must be non-negative.";
            if (s.X > 10000 || s.Y > 10000) yield return "Coordinates seem unusually large.";
            if (!Enum.IsDefined(typeof(MouseButton), s.Button)) yield return "Invalid mouse button value.";
        }

        public IAutoToolCommand CreateCommand(AutoToolCommandSettings settings, IServiceProvider _)
            => new ClickCommand((ClickSettings)settings);
    }
}