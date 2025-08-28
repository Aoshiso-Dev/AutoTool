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
            var cloneItems = (IEnumerable<ICommandListItem>)items.Select(x => x.Clone()).ToList();
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
                if (command != null) commands.Add(command);
            }
            return commands;
        }

        private static ICommand? CreateCommand(ICommand parent, ICommandListItem item, IEnumerable<ICommandListItem> items)
        {
            if (item.IsInLoop || item.IsInIf) return null;
            if (CommandRegistry.TryCreateSimple(parent, item, out var simple)) return simple;
            return item switch
            {
                IfImageExistItem exist => CreateIfCommand(parent, exist, items),
                IfImageNotExistItem notExist => CreateIfCommand(parent, notExist, items),
                IfImageExistAIItem aiExist => CreateIfCommand(parent, aiExist, items),
                IfImageNotExistAIItem aiNotExist => CreateIfCommand(parent, aiNotExist, items),
                LoopItem loop => CreateLoopComand(parent, loop, items),
                EndLoopItem => null,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static ICommand CreateIfCommand(ICommand parent, IIfItem ifItem, IEnumerable<ICommandListItem> items)
        {
            var endIfItem = ifItem.Pair as ICommandListItem ?? throw new Exception("ifItem.Pair is null");
            var childrenListItems = items.Where(x => x.LineNumber > ifItem.LineNumber && x.LineNumber < endIfItem.LineNumber).ToList();
            if (childrenListItems.Count == 0) throw new Exception("childrenListItems.Count is 0");

            IIfCommand ifCommand = ifItem switch
            {
                IfImageExistItem exist => new IfImageExistCommand(parent, exist) { LineNumber = exist.LineNumber, IsEnabled = exist.IsEnable },
                IfImageNotExistItem notExist => new IfImageNotExistCommand(parent, notExist){ LineNumber = notExist.LineNumber, IsEnabled = notExist.IsEnable },
                IfImageExistAIItem existAI => new IfImageExistAICommand(parent, existAI) { LineNumber = existAI.LineNumber, IsEnabled = existAI.IsEnable },
                IfImageNotExistAIItem notExistAI => new IfImageNotExistAICommand(parent, notExistAI) { LineNumber = notExistAI.LineNumber, IsEnabled = notExistAI.IsEnable },
                _ => throw new ArgumentOutOfRangeException()
            };

            ifCommand.Children = ListItemToCommand(ifCommand, childrenListItems);
            childrenListItems.Where(x => x.NestLevel == ifItem.NestLevel + 1).ToList().ForEach(x => x.IsInIf = true);
            return ifCommand;
        }

        private static LoopCommand CreateLoopComand(ICommand parent, ILoopItem loopItem, IEnumerable<ICommandListItem> items)
        {
            var endLoopItem = loopItem.Pair as ICommandListItem ?? throw new Exception("対になるEndLoopが見つかりません。");
            var childrenListItems = items.Where(x => x.LineNumber > loopItem.LineNumber && x.LineNumber < endLoopItem.LineNumber).ToList();
            if (childrenListItems.Count == 0) throw new Exception("Loopの中に要素がありません。");
            var endLoopCommand = parent.Children.FirstOrDefault(x => x.LineNumber == loopItem.Pair?.LineNumber) as EndLoopCommand;
            var loopCommand = new LoopCommand(parent, new LoopCommandSettings() { LoopCount = loopItem.LoopCount, Pair = endLoopCommand })
            {
                LineNumber = loopItem.LineNumber,
                IsEnabled = loopItem.IsEnable,
            };
            loopCommand.Children = ListItemToCommand(loopCommand, childrenListItems);
            childrenListItems.Where(x => x.NestLevel == loopItem.NestLevel + 1).ToList().ForEach(x => x.IsInLoop = true);
            return loopCommand;
        }

        private static EndLoopCommand CreateEndLoopCommand(ICommand parent, IEndLoopItem endLoopItem, IEnumerable<ICommandListItem> items)
        {
            var loopItem = endLoopItem.Pair as LoopItem ?? throw new Exception("対になるLoopが見つかりません。");
            var loopCommand = parent.Children.FirstOrDefault(x => x.LineNumber == loopItem.LineNumber) ?? throw new Exception("対になるLoopCommandが見つかりません。");
            return new EndLoopCommand(parent, new EndLoopCommandSettings() { Pair = loopCommand })
            {
                LineNumber = endLoopItem.LineNumber,
                Children = loopCommand.Children,
                IsEnabled = endLoopItem.IsEnable,
            };
        }
    }
}
