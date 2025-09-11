using AutoTool.Core.Abstractions;
using AutoTool.Core.Commands;
using AutoTool.Core.Descriptors;
using System.Windows.Input;

namespace AutoTool.Commands.Input.KeyInput
{
    /// <summary>
    /// KeyInputコマンドのディスクリプタ
    /// </summary>
    public sealed class KeyInputDescriptor : ICommandDescriptor
    {
        public string Type => "keyinput";
        public string DisplayName => "キー入力";
        public string? IconKey => "mdi:keyboard";

        public Type SettingsType => typeof(KeyInputSettings);
        public int LatestSettingsVersion => 1;

        public IReadOnlyList<BlockSlot> BlockSlots { get; } = Array.Empty<BlockSlot>();

        public AutoToolCommandSettings CreateDefaultSettings() => new KeyInputSettings
        {
            Key = Key.Enter,
            Ctrl = false,
            Alt = false,
            Shift = false,
            Description = "Enterキー入力"
        };

        public AutoToolCommandSettings MigrateToLatest(AutoToolCommandSettings settings)
        {
            if (settings is KeyInputSettings s)
                return s with { Version = LatestSettingsVersion };
            throw new InvalidCastException($"Unexpected settings type: {settings.GetType().Name}");
        }

        public IEnumerable<string> ValidateSettings(AutoToolCommandSettings settings)
        {
            if (settings is not KeyInputSettings s) { yield return "Settings type mismatch."; yield break; }
            
            if (s.Key == Key.None) yield return "Valid key must be specified.";
            
            // 危険なキーの組み合わせをチェック
            if (s.Ctrl && s.Alt && s.Key == Key.Delete) 
                yield return "Ctrl+Alt+Delete is a dangerous key combination.";
            
            if (s.Alt && s.Key == Key.F4) 
                yield return "Alt+F4 may close the application.";
        }

        public IAutoToolCommand CreateCommand(AutoToolCommandSettings settings, IServiceProvider _)
            => new KeyInputCommand((KeyInputSettings)settings);
    }
}