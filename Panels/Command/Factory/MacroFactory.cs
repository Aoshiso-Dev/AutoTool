using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Panels.Command.Interface;
using Panels.Command.Class;
using Panels.Command.Define;
using Panels.List;
using Panels.List.Interface;
using Panels.List.Class;
using System.Windows;

namespace Panels.Command.Factory
{
    public static class MacroFactory
    {
        public static ICommand CreateMacro(IEnumerable<ICommandListItem> items, EventHandler<int> updateRunning)
        {
            // itemsの内容をコピーする
            var cloneItems = (IEnumerable<ICommandListItem>)items.Select(x => x.Clone()).ToList();

            // ルートコマンドを作成
            var root = new RootCommand();
            root.OnCommandRunning += updateRunning;

            // ルートコマンドの子コマンドを作成
            root.Children = ListItemToCommand(root, cloneItems, updateRunning);

            return root;
        }

        private static IEnumerable<ICommand> ListItemToCommand(ICommand parent, IEnumerable<ICommandListItem> items, EventHandler<int> updateRunning)
        {
            var commands = new List<ICommand>();

            foreach (var item in items)
            {
                var command = CreateCommand(parent, item, items, updateRunning);

                if (command != null)
                {
                    commands.Add(command);
                    parent = command;
                }
            }

            return commands;
        }

        private static ICommand? CreateCommand(ICommand parent, ICommandListItem item, IEnumerable<ICommandListItem> items, EventHandler<int> updateRunnning)
        {
            if(item.IsInLoop)
            {
                // Do nothing
                return new BaseCommand(parent, new CommandSettings());
            }

            ICommand command;

            switch(item)
            {
                case WaitItem waitItem:
                    command = CreateWaitComand(parent, waitItem);
                    break;
                case ClickItem clickItem:
                    command = CreateClickComand(parent, clickItem);
                    break;
                case HotkeyItem hotkeyItem:
                    command = CreateHotkeyComand(parent, hotkeyItem);
                    break;
                case ClickImageItem clickImageItem:
                    command = CreateClickImageComand(parent, clickImageItem);
                    break;
                case WaitImageItem waitImageItem:
                    command = CreateWaitImageComand(parent, waitImageItem);
                    break;
                case IfItem ifItem:
                    command = CreateIfComand(parent, ifItem);
                    break;
                case EndIfItem endIfItem:
                    // Do nothing
                    return new BaseCommand(parent, new CommandSettings());
                case LoopItem loopItem:
                    command = CreateLoopComand(parent, loopItem, items, updateRunnning);
                    break;
                case EndLoopItem endLoopItem:
                    // Do nothing
                    return new BaseCommand(parent, new CommandSettings());
                default:
                    throw new ArgumentOutOfRangeException();
            }


            command.OnCommandRunning += updateRunnning;

            return command;

            /*

            if (item is WaitItem waitItem)
            {
                return CreateWaitComand(parent, waitItem);
            }
            else if (item is ClickItem clickItem)
            {
                return CreateClickComand(parent, clickItem);
            }
            else if (item is HotkeyItem hotkeyItem)
            {
                return CreateHotkeyComand(parent, hotkeyItem);
            }
            else if (item is ClickImageItem clickImageItem)
            {
                return CreateClickImageComand(parent, clickImageItem);
            }
            else if (item is WaitImageItem waitImageItem)
            {
                return CreateWaitImageComand(parent, waitImageItem);
            }
            else if (item is IfItem ifItem)
            {
                return CreateIfComand(parent, ifItem);
            }
            else if (item is EndIfItem endIfItem)
            {
                // Do nothing
                return new BaseCommand(parent, new CommandSettings());
            }
            else if (item is LoopItem loopItem)
            {
                return CreateLoopComand(parent, loopItem, items);
            }
            else if (item is EndLoopItem endLoopItem)
            {
                // Do nothing
                return new BaseCommand(parent, new CommandSettings());
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
            */
        }

        private static WaitImageCommand CreateWaitImageComand(ICommand parent, IWaitImageItem item)
        {
            return new WaitImageCommand(parent, new ImageCommandSettings()
            {
                ImagePath = item.ImagePath,
                Threshold = item.Threshold,
                Timeout = item.Timeout,
                Interval = item.Interval,
            })
            { ListNumber = item.LineNumber };
        }

        private static ClickImageCommand CreateClickImageComand(ICommand parent, IClickImageItem item)
        {
            return new ClickImageCommand(parent, new ImageCommandSettings()
            {
                ImagePath = item.ImagePath,
                Threshold = item.Threshold,
                Timeout = item.Timeout,
                Interval = item.Interval,
            })
            { ListNumber = item.LineNumber };
        }

        private static HotkeyCommand CreateHotkeyComand(ICommand parent, IHotkeyItem item)
        {
            return new HotkeyCommand(parent, new HotkeyCommandSettings()
            {
                Ctrl = item.Ctrl,
                Alt = item.Alt,
                Shift = item.Shift,
                Key = item.Key,
            })
            { ListNumber = item.LineNumber };
        }

        private static ClickCommand CreateClickComand(ICommand parent, IClickItem item)
        {
            return new ClickCommand(parent, new ClickCommandSettings()
            {
                Button = item.Button,
                X = item.X,
                Y = item.Y,
            })
            { ListNumber = item.LineNumber };
        }

        private static WaitCommand CreateWaitComand(ICommand parent, IWaitItem item)
        {
            return new WaitCommand(parent, new WaitCommandSettings()
            {
                Wait = item.Wait,
            })
            { ListNumber = item.LineNumber };
        }

        private static IfCommand? CreateIfComand(ICommand parent, IIfItem item)
        {
            // TODO

            return null ?? new IfCommand(parent, new IfCommandSettings() { Condition = null});
        }

        private static LoopCommand CreateLoopComand(ICommand parent, ILoopItem loopItem, IEnumerable<ICommandListItem> items, EventHandler<int> updateRunning)
        {
            var endLoopItem = loopItem.Pair as ICommandListItem;

            if (endLoopItem == null)
            {
                throw new Exception("loopItem.Pair is null");
            }

            var childrenListItems = items.SkipWhile(x => x != loopItem).TakeWhile(x => x != endLoopItem).Skip(1).ToList();

            if (childrenListItems.Count == 0)
            {
                throw new Exception("childrenListItems.Count is 0");
            }

            var loopCommand = new LoopCommand(parent, new LoopCommandSettings()
            {
                LoopCount = loopItem.LoopCount,
            })
            {
                ListNumber = loopItem.LineNumber,
            };

            if (childrenListItems.Any())
            {
                loopCommand.Children = ListItemToCommand(loopCommand, childrenListItems, updateRunning);
            }

            // IsInLoopをtrueにする
            foreach (var childrenListItem in childrenListItems)
            {
                childrenListItem.IsInLoop = true;
            }

            /*
            foreach (var childrenListItem in childrenListItems)
            {
                if(childrenListItem is ILoopItem)
                {
                    continue;
                }
                else if(childrenListItem is IEndLoopItem)
                {
                    continue;
                }

                if (childrenListItem.NestLevel == loopItem.NestLevel + 1)
                {
                    childrenListItem.IsInLoop = true;
                }
            }
            */

            return loopCommand;
        }
    }
}
