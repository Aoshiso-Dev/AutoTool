using MacroPanels.Command.Interface;
using MacroPanels.Command.Class;
using MacroPanels.List.Class;
using System.Diagnostics;
using MacroPanels.Model.List.Interface;

namespace MacroPanels.Model.MacroFactory
{
    public static class MacroFactory
    {
        public static ICommand CreateMacro(IEnumerable<ICommandListItem> items)
        {
            // 実行時にUIと干渉しないようクローン
            var cloneItems = (IEnumerable<ICommandListItem>)items.Select(x => x.Clone()).ToList();

            // ルート（1回ループでラップ）
            var root = new LoopCommand(new RootCommand(), new LoopCommandSettings() { LoopCount = 1 });

            root.Children = ListItemToCommand(root, cloneItems);
            return root;
        }

        private static IEnumerable<ICommand> ListItemToCommand(ICommand parent, IEnumerable<ICommandListItem> items)
        {
            var commands = new List<ICommand>();

            foreach (var item in items)
            {
                if (!item.IsEnable) continue;

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
            if (item.IsInLoop) return null;
            if (item.IsInIf) return null;

            // 単純コマンドはレジストリから生成
            if (CommandRegistry.TryCreateSimple(parent, item, out var simple))
            {
                return simple;
            }

            // 構造コマンド（子やペア解決が必要なもの）
            return item switch
            {
                IfImageExistItem exist => CreateIfCommand(parent, exist, items),
                IfImageNotExistItem notExist => CreateIfCommand(parent, notExist, items),
                LoopItem loop => CreateLoopComand(parent, loop, items),
                EndLoopItem => null, // EndLoop は Loop 生成時に解決
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static ICommand CreateIfCommand(ICommand parent, IIfItem ifItem, IEnumerable<ICommandListItem> items)
        {
            var endIfItem = ifItem.Pair as ICommandListItem
                ?? throw new Exception("ifItem.Pair is null");

            // if ～ endIf の内側を子として構築
            var childrenListItems = items.Where(x => x.LineNumber > ifItem.LineNumber && x.LineNumber < endIfItem.LineNumber).ToList();
            if (childrenListItems.Count == 0)
            {
                throw new Exception("childrenListItems.Count is 0");
            }

            IIfCommand ifCommand = ifItem switch
            {
                IfImageExistItem exist => new IfImageExistCommand(parent, (IWaitImageCommandSettings)exist)
                {
                    LineNumber = exist.LineNumber,
                    IsEnabled = exist.IsEnable,
                },
                IfImageNotExistItem notExist => new IfImageNotExistCommand(parent, (IWaitImageCommandSettings)notExist)
                {
                    LineNumber = notExist.LineNumber,
                    IsEnabled = notExist.IsEnable,
                },
                _ => throw new ArgumentOutOfRangeException()
            };

            ifCommand.Children = ListItemToCommand(ifCommand, childrenListItems);

            // ネストフラグを付与（既存踏襲）
            childrenListItems.Where(x => x.NestLevel == ifItem.NestLevel + 1)
                             .ToList()
                             .ForEach(x => x.IsInIf = true);

            return ifCommand;
        }

        private static LoopCommand CreateLoopComand(ICommand parent, ILoopItem loopItem, IEnumerable<ICommandListItem> items)
        {
            var endLoopItem = loopItem.Pair as ICommandListItem
                ?? throw new Exception("対になるEndLoopが見つかりません。");

            var childrenListItems = items.Where(x => x.LineNumber > loopItem.LineNumber && x.LineNumber < endLoopItem.LineNumber).ToList();
            if (childrenListItems.Count == 0)
            {
                throw new Exception("Loopの中に要素がありません。");
            }

            var endLoopCommand = parent.Children.FirstOrDefault(x => x.LineNumber == loopItem.Pair?.LineNumber) as EndLoopCommand;

            var loopCommand = new LoopCommand(parent, new LoopCommandSettings()
            {
                LoopCount = loopItem.LoopCount,
                Pair = endLoopCommand,
            })
            {
                LineNumber = loopItem.LineNumber,
                IsEnabled = loopItem.IsEnable,
            };

            loopCommand.Children = ListItemToCommand(loopCommand, childrenListItems);

            childrenListItems.Where(x => x.NestLevel == loopItem.NestLevel + 1)
                             .ToList()
                             .ForEach(x => x.IsInLoop = true);

            return loopCommand;
        }

        private static EndLoopCommand CreateEndLoopCommand(ICommand parent, IEndLoopItem endLoopItem, IEnumerable<ICommandListItem> items)
        {
            var loopItem = endLoopItem.Pair as LoopItem
                ?? throw new Exception("対になるLoopが見つかりません。");

            var loopCommand = parent.Children.FirstOrDefault(x => x.LineNumber == loopItem.LineNumber)
                ?? throw new Exception("対になるLoopCommandが見つかりません。");

            return new EndLoopCommand(parent, new EndLoopCommandSettings() { Pair = loopCommand })
            {
                LineNumber = endLoopItem.LineNumber,
                Children = loopCommand.Children,
                IsEnabled = endLoopItem.IsEnable,
            };
        }
    }
}
