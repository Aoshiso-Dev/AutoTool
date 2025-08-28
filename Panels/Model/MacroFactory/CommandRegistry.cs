using MacroPanels.Command.Class;
using MacroPanels.Command.Interface;
using MacroPanels.List.Class;
using MacroPanels.Model.List.Interface;
using System;
using System.Collections.Generic;

namespace MacroPanels.Model.MacroFactory
{
    /// <summary>
    /// �V���v���i�q��y�A�������s�v�j�ȃR�}���h�̐������^�o�^�ŊǗ����郌�W�X�g���B
    /// �V�K�R�}���h�� RegisterDefaults ��1�s�ǉ����邾���őΉ��\�B
    /// </summary>
    public static class CommandRegistry
    {
        // Item �̌^ -> Command �����֐� �̃}�b�v
        private static readonly Dictionary<Type, Func<ICommand, ICommandListItem, ICommand>> s_simpleMap = new();

        static CommandRegistry()
        {
            RegisterDefaults();
        }

        /// <summary>
        /// �P���R�}���h��o�^����B
        /// </summary>
        public static void RegisterSimple<TItem>(Func<ICommand, TItem, ICommand> factory)
            where TItem : ICommandListItem
        {
            s_simpleMap[typeof(TItem)] = (parent, item) => factory(parent, (TItem)item);
        }

        /// <summary>
        /// �P���R�}���h�̐��������݂�B�����ł����ꍇ�� LineNumber/IsEnabled �𔽉f����B
        /// </summary>
        public static bool TryCreateSimple(ICommand parent, ICommandListItem item, out ICommand? command)
        {
            if (s_simpleMap.TryGetValue(item.GetType(), out var creator))
            {
                command = creator(parent, item);
                // ���ʂ̃��^���𔽉f
                command.LineNumber = item.LineNumber;
                command.IsEnabled = item.IsEnable;
                return true;
            }

            command = null;
            return false;
        }

        private static void RegisterDefaults()
        {
            // ListItem(= Settings ����) �����̂܂� Command �֓n��
            RegisterSimple<WaitImageItem>((p, it) => new WaitImageCommand(p, (IWaitImageCommandSettings)it));
            RegisterSimple<ClickImageItem>((p, it) => new ClickImageCommand(p, (IClickImageCommandSettings)it));
            RegisterSimple<HotkeyItem>((p, it) => new HotkeyCommand(p, (IHotkeyCommandSettings)it));
            RegisterSimple<ClickItem>((p, it) => new ClickCommand(p, (IClickCommandSettings)it));
            RegisterSimple<WaitItem>((p, it) => new WaitCommand(p, (IWaitCommandSettings)it));
            RegisterSimple<BreakItem>((p, it) => new BreakCommand(p, new CommandSettings()));
        }
    }
}
