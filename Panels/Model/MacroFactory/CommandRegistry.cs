using MacroPanels.Command.Class;
using MacroPanels.Command.Interface;
using MacroPanels.List.Class;
using MacroPanels.Model.List.Interface;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MacroPanels.Model.MacroFactory
{
    /// <summary>
    /// シンプル（子やペア解決が不要）なコマンドの生成を、属性ベースで自動登録するレジストリ。
    /// </summary>
    public static class CommandRegistry
    {
        // Item の型 -> Command 生成関数 のマップ
        private static readonly Dictionary<Type, Func<ICommand, ICommandListItem, ICommand>> s_simpleMap = new();

        static CommandRegistry()
        {
            // 現在のアセンブリ内の Item を属性から一括登録
            RegisterFromAssembly(typeof(CommandRegistry).Assembly);
        }

        /// <summary>
        /// 単純コマンドの生成を試みる。生成できた場合は LineNumber/IsEnabled を反映する。
        /// </summary>
        public static bool TryCreateSimple(ICommand parent, ICommandListItem item, out ICommand? command)
        {
            if (s_simpleMap.TryGetValue(item.GetType(), out var creator))
            {
                command = creator(parent, item);
                // 共通のメタ情報を反映
                command.LineNumber = item.LineNumber;
                command.IsEnabled = item.IsEnable;
                return true;
            }

            command = null;
            return false;
        }

        /// <summary>
        /// Attribute に基づき、アセンブリから単純コマンドを自動登録する。
        /// </summary>
        public static void RegisterFromAssembly(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!typeof(ICommandListItem).IsAssignableFrom(type)) continue;

                var attr = type.GetCustomAttribute<SimpleCommandBindingAttribute>();
                if (attr == null) continue;

                var commandCtor = attr.CommandType.GetConstructor(new[] { typeof(ICommand), typeof(ICommandSettings) })
                                  ?? throw new InvalidOperationException($"コマンド型 {attr.CommandType.Name} に (ICommand, ICommandSettings) コンストラクタがありません。");

                s_simpleMap[type] = (parent, item) =>
                {
                    if (!attr.SettingsInterfaceType.IsInstanceOfType(item))
                    {
                        throw new InvalidOperationException($"{item.GetType().Name} は {attr.SettingsInterfaceType.Name} を実装していません。");
                    }
                    return (ICommand)commandCtor.Invoke(new object[] { parent, (ICommandSettings)item });
                };
            }
        }
    }
}
