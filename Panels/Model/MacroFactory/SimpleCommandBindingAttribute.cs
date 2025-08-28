using System;

namespace MacroPanels.Model.MacroFactory
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class SimpleCommandBindingAttribute : Attribute
    {
        public Type CommandType { get; }
        public Type SettingsInterfaceType { get; }

        public SimpleCommandBindingAttribute(Type commandType, Type settingsInterfaceType)
        {
            CommandType = commandType ?? throw new ArgumentNullException(nameof(commandType));
            SettingsInterfaceType = settingsInterfaceType ?? throw new ArgumentNullException(nameof(settingsInterfaceType));
        }
    }
}
