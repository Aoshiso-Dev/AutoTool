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
        public static ICommand CreateMacro(IEnumerable<ICommandListItem> items)
        {
            // itemsの内容をコピーする
            var cloneItems = (IEnumerable<ICommandListItem>)items.Select(x => x.Clone()).ToList();

            var root = new RootCommand();
            var commands = ListItemToCommand(root, cloneItems);

            root.Children = commands;

            return root;
        }

        private static IEnumerable<ICommand> ListItemToCommand(ICommand parent, IEnumerable<ICommandListItem> items)
        {
            var commands = new List<ICommand>();

            foreach (var item in items)
            {
                var command = CreateCommand(parent, item, items);

                if (command != null)
                {
                    commands.Add(command);
                    parent = command;
                }
            }

            return commands;
        }

        private static ICommand? CreateCommand(ICommand parent, ICommandListItem item, IEnumerable<ICommandListItem> items)
        {
            if(item.IsInLoop)
            {
                // Do nothing
                return new BaseCommand(parent, new CommandSettings());
            }

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
        }

        private static WaitImageCommand CreateWaitImageComand(ICommand parent, IWaitImageItem item)
        {
            return new WaitImageCommand(parent, new ImageCommandSettings()
            {
                ImagePath = item.ImagePath,
                Threshold = item.Threshold,
                Timeout = item.Timeout,
                Interval = item.Interval,
            });
        }

        private static ClickImageCommand CreateClickImageComand(ICommand parent, IClickImageItem item)
        {
            return new ClickImageCommand(parent, new ImageCommandSettings()
            {
                ImagePath = item.ImagePath,
                Threshold = item.Threshold,
                Timeout = item.Timeout,
                Interval = item.Interval,
            });
        }

        private static HotkeyCommand CreateHotkeyComand(ICommand parent, IHotkeyItem item)
        {
            return new HotkeyCommand(parent, new HotkeyCommandSettings()
            {
                Ctrl = item.Ctrl,
                Alt = item.Alt,
                Shift = item.Shift,
                Key = item.Key,
            });
        }

        private static ClickCommand CreateClickComand(ICommand parent, IClickItem item)
        {
            return new ClickCommand(parent, new ClickCommandSettings()
            {
                Button = item.Button,
                X = item.X,
                Y = item.Y,
            });
        }

        private static WaitCommand CreateWaitComand(ICommand parent, IWaitItem item)
        {
            return new WaitCommand(parent, new WaitCommandSettings()
            {
                Wait = item.Wait,
            });
        }

        private static IfCommand? CreateIfComand(ICommand parent, IIfItem item)
        {
            // TODO

            return null ?? new IfCommand(parent, new IfCommandSettings() { Condition = null});
        }

        private static LoopCommand CreateLoopComand(ICommand parent, ILoopItem loopItem, IEnumerable<ICommandListItem> items)
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
            });

            if (childrenListItems.Any())
            {
                loopCommand.Children = ListItemToCommand(loopCommand, childrenListItems);
            }

            // IsInLoopをtrueにする
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

            return loopCommand;

            /*
            var endLoopItem = loopItem.Pair as ICommandListItem;

            if (endLoopItem == null)
            {
                throw new Exception("loopItem.Pair is null");
            }

            var childrenListItems = items.SkipWhile(x => x != loopItem).TakeWhile(x => x != endLoopItem).Skip(1).ToList();

            if (childrenListItems.Count() == 0)
            {
                throw new Exception("childrenListItems.Count is 0");
            }

            // itemsからchildrenListItemsの要素を削除
            items = items.Except(childrenListItems);

            //MessageBox.Show("childrenFirst=" + childrenListItems.First().LineNumber.ToString() + "\n" + "childrenLast=" + childrenListItems.Last().LineNumber.ToString() + "\n" + "childCount=" + childrenListItems.Count.ToString());

            var loopCommand = new LoopCommand(parent, new LoopCommandSettings()
            {
                LoopCount = loopItem.LoopCount,
            });

            MessageBox.Show("loopCount=" + loopItem.LoopCount.ToString());

            if (childrenListItems.Any())
            {
                loopCommand.Children = ListItemToCommand(loopCommand, childrenListItems);
            }

            return loopCommand;
            */
        }
    }
}
