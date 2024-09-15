using Command.Interface;
using Command.Class;
using Panels.List.Class;
using System.Diagnostics;
using Panels.Model.List.Interface;

namespace Panels.Model.MacroFactory
{
    public static class MacroFactory
    {
        public static ICommand CreateMacro(IEnumerable<ICommandListItem> items)
        {
            // itemsの内容をコピーする
            var cloneItems = (IEnumerable<ICommandListItem>)items.Select(x => x.Clone()).ToList();

            // ルートコマンドを作成
            var root = new LoopCommand(new RootCommand(), new LoopCommandSettings() { LoopCount = 1 });

            // ルートコマンドの子コマンドを作成
            root.Children = ListItemToCommand(root, cloneItems);

            return root;
        }

        private static IEnumerable<ICommand> ListItemToCommand(ICommand parent, IEnumerable<ICommandListItem> items)
        {
            var commands = new List<ICommand>();

            foreach (var item in items)
            {
                Debug.WriteLine($"Create: {item.LineNumber}, {item.ItemType}");
                var command = CreateCommand(parent, item, items);

                if (command != null)
                {
                    commands.Add(command);
                }
            }

            return commands;
        }

        private static ICommand? CreateCommand(ICommand parent, ICommandListItem item, IEnumerable<ICommandListItem> items)
        {
            if (item.IsInLoop)
            {
                return null;
            }

            if (item.IsInIf)
            {
                return new BaseCommand(parent, new CommandSettings());
            }

            ICommand? command;

            switch (item)
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
                case IfImageExistItem ifImageExistItem:
                    command = CreateIfCommand(parent, ifImageExistItem, items);
                    break;
                case IfImageNotExistItem ifImageNotExistItem:
                    command = CreateIfCommand(parent, ifImageNotExistItem, items);
                    break;
                case EndIfItem endIfItem:
                    command = null;
                    break;
                case LoopItem loopItem:
                    command = CreateLoopComand(parent, loopItem, items);
                    break;
                case EndLoopItem endLoopItem:
                    command = null;
                    break;
                case BreakItem breakItem:
                    command = new BreakCommand(parent, new CommandSettings());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return command;
        }

        private static WaitImageCommand CreateWaitImageComand(ICommand parent, IWaitImageItem item)
        {
            return new WaitImageCommand(parent, new WaitImageCommandSettings()
            {
                ImagePath = item.ImagePath,
                Threshold = item.Threshold,
                Timeout = item.Timeout,
                Interval = item.Interval,
            })
            { LineNumber = item.LineNumber };
        }

        private static ClickImageCommand CreateClickImageComand(ICommand parent, IClickImageItem item)
        {
            return new ClickImageCommand(parent, new ClickImageCommandSettings()
            {
                ImagePath = item.ImagePath,
                Threshold = item.Threshold,
                Timeout = item.Timeout,
                Interval = item.Interval,
                Button = item.Button,
            })
            { LineNumber = item.LineNumber };
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
            { LineNumber = item.LineNumber };
        }

        private static ClickCommand CreateClickComand(ICommand parent, IClickItem item)
        {
            return new ClickCommand(parent, new ClickCommandSettings()
            {
                Button = item.Button,
                X = item.X,
                Y = item.Y,
            })
            { LineNumber = item.LineNumber };
        }

        private static WaitCommand CreateWaitComand(ICommand parent, IWaitItem item)
        {
            return new WaitCommand(parent, new WaitCommandSettings()
            {
                Wait = item.Wait,
            })
            { LineNumber = item.LineNumber };
        }

        private static ICommand CreateIfCommand(ICommand parent, IIfItem ifItem, IEnumerable<ICommandListItem> items)
        {
            // endIfItemを取得
            var endIfItem = ifItem.Pair as ICommandListItem;
            if (endIfItem == null)
            {
                throw new Exception("ifItem.Pair is null");
            }

            // ifItemとendIfItemの間のコマンドを取得
            var childrenListItems = items.Where(x => x.LineNumber > ifItem.LineNumber && x.LineNumber < endIfItem.LineNumber).ToList();
            if (childrenListItems.Count == 0)
            {
                throw new Exception("childrenListItems.Count is 0");
            }

            // IfCommandを作成
            IIfCommand ifCommand = new IfCommand(new RootCommand(), new CommandSettings());
            switch (ifItem)
            {
                case IfImageExistItem ifImageExistItem:
                    ifCommand = new IfImageExistCommand(parent, new WaitImageCommandSettings()
                    {
                        ImagePath = ifImageExistItem.ImagePath,
                        Threshold = ifImageExistItem.Threshold,
                        Timeout = ifImageExistItem.Timeout,
                        Interval = ifImageExistItem.Interval,
                    })
                    {
                        LineNumber = ifImageExistItem.LineNumber,
                    };
                    break;
                case IfImageNotExistItem ifImageNotExistItem:
                    ifCommand = new IfImageNotExistCommand(parent, new WaitImageCommandSettings()
                    {
                        ImagePath = ifImageNotExistItem.ImagePath,
                        Threshold = ifImageNotExistItem.Threshold,
                        Timeout = ifImageNotExistItem.Timeout,
                        Interval = ifImageNotExistItem.Interval,
                    })
                    {
                        LineNumber = ifImageNotExistItem.LineNumber,
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // IfCommandの子コマンドを作成
            ifCommand.Children = ListItemToCommand(ifCommand, childrenListItems);

            // IsInIfをtrueにする
            childrenListItems.Where(x => x.NestLevel == ifItem.NestLevel + 1).ToList().ForEach(x => x.IsInIf = true);

            return ifCommand;
        }

        private static LoopCommand CreateLoopComand(ICommand parent, ILoopItem loopItem, IEnumerable<ICommandListItem> items)
        {
            // endLoopItemを取得
            var endLoopItem = loopItem.Pair as ICommandListItem;

            if (endLoopItem == null)
            {
                throw new Exception("loopItem.Pair is null");
            }

            // loopItemとendLoopItemの間のコマンドを取得
            var childrenListItems = items.Where(x => x.LineNumber > loopItem.LineNumber && x.LineNumber < endLoopItem.LineNumber).ToList();

            if (childrenListItems.Count == 0)
            {
                throw new Exception("childrenListItems.Count is 0");
            }

            // LoopCommandを作成
            var loopCommand = new LoopCommand(parent, new LoopCommandSettings()
            {
                LoopCount = loopItem.LoopCount,
            })
            {
                LineNumber = loopItem.LineNumber,
            };

            // LoopCommandの子コマンドを作成
            loopCommand.Children = ListItemToCommand(loopCommand, childrenListItems);

            // IsInLoopをtrueにする
            childrenListItems.Where(x => x.NestLevel == loopItem.NestLevel + 1).ToList().ForEach(x => x.IsInLoop = true);


            return loopCommand;
        }
    }
}
