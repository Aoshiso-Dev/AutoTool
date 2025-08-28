using MacroPanels.Command.Class;
using MacroPanels.Command.Interface;
using MacroPanels.List.Class;
using MacroPanels.Model.List.Interface;
using System;
using System.Collections.Generic;

namespace MacroPanels.Model.MacroFactory
{
    /// <summary>
    /// シンプル（子やペア解決が不要）なコマンドの生成を型登録で管理するレジストリ。
    /// 新規コマンドは RegisterDefaults に1行追加するだけで対応可能。
    /// </summary>
    public static class CommandRegistry
    {
        // Item の型 -> Command 生成関数 のマップ
        private static readonly Dictionary<Type, Func<ICommand, ICommandListItem, ICommand>> s_simpleMap = new();

        static CommandRegistry()
        {
            RegisterDefaults();
        }

        /// <summary>
        /// 単純コマンドを登録する。
        /// </summary>
        public static void RegisterSimple<TItem>(Func<ICommand, TItem, ICommand> factory)
            where TItem : ICommandListItem
        {
            s_simpleMap[typeof(TItem)] = (parent, item) => factory(parent, (TItem)item);
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

        private static void RegisterDefaults()
        {
            // ListItem(= Settings 実装) をそのまま Command へ渡す
            RegisterSimple<WaitImageItem>((p, it) => new WaitImageCommand(p, (IWaitImageCommandSettings)it));
            RegisterSimple<ClickImageItem>((p, it) => new ClickImageCommand(p, (IClickImageCommandSettings)it));
            RegisterSimple<HotkeyItem>((p, it) => new HotkeyCommand(p, (IHotkeyCommandSettings)it));
            RegisterSimple<ClickItem>((p, it) => new ClickCommand(p, (IClickCommandSettings)it));
            RegisterSimple<WaitItem>((p, it) => new WaitCommand(p, (IWaitCommandSettings)it));
            RegisterSimple<BreakItem>((p, it) => new BreakCommand(p, new CommandSettings()));
        }
    }
}
