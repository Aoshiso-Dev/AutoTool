using MacroPanels.Command.Interface;
using MacroPanels.Command.Class;
using MacroPanels.List.Class;
using System.Diagnostics;
using MacroPanels.Model.List.Interface;
using MacroPanels.Model.CommandDefinition;

namespace MacroPanels.Model.MacroFactory
{
    public static class MacroFactory
    {
        public static ICommand CreateMacro(IEnumerable<ICommandListItem> items)
        {
            var cloneItems = items.Select(x => x.Clone()).ToList();
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
                
                try
                {
                    var command = CreateCommand(parent, item, items);
                    if (command != null) commands.Add(command);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error creating command for {item.ItemType} at line {item.LineNumber}: {ex.Message}");
                    throw new InvalidOperationException($"コマンド '{item.ItemType}' (行 {item.LineNumber}) の生成に失敗しました", ex);
                }
            }
            return commands;
        }

        private static ICommand? CreateCommand(ICommand parent, ICommandListItem item, IEnumerable<ICommandListItem> items)
        {
            if (item.IsInLoop || item.IsInIf) return null;
            
            Debug.WriteLine($"Creating command for ItemType: {item.ItemType}, Type: {item.GetType().Name}");
            
            // CommandRegistryの初期化を確実に実行
            MacroPanels.Model.CommandDefinition.CommandRegistry.Initialize();
            
            // 属性ベースの単純コマンドを優先
            if (MacroPanels.Model.CommandDefinition.CommandRegistry.TryCreateSimple(parent, item, out var simple)) 
            {
                Debug.WriteLine($"Successfully created simple command: {simple.GetType().Name}");
                return simple;
            }

            Debug.WriteLine($"Could not create simple command for {item.ItemType}, checking complex commands...");
            
            // 複合コマンド（条件分岐・ループ）の処理
            switch (item)
            {
                case IIfItem ifItem:
                    Debug.WriteLine($"Creating If command for {item.ItemType}");
                    return CreateIfCommand(parent, ifItem, items);
                
                case ILoopItem loopItem:
                    Debug.WriteLine($"Creating Loop command for {item.ItemType}");
                    return CreateLoopCommand(parent, loopItem, items);
                
                default:
                    Debug.WriteLine($"Unsupported item type: {item.GetType().Name} (ItemType: {item.ItemType})");
                    throw new NotSupportedException($"未対応のアイテム型です: {item.GetType().Name} (ItemType: {item.ItemType})");
            }
        }

        private static ICommand CreateIfCommand(ICommand parent, IIfItem ifItem, IEnumerable<ICommandListItem> items)
        {
            if (ifItem.Pair == null)
                throw new InvalidOperationException($"If文 (行 {ifItem.LineNumber}) に対応するEndIfがありません");

            var endIfItem = ifItem.Pair;
            var childrenListItems = items
                .Where(x => x.LineNumber > ifItem.LineNumber && x.LineNumber < endIfItem.LineNumber)
                .ToList();

            if (childrenListItems.Count == 0)
                throw new InvalidOperationException($"If文 (行 {ifItem.LineNumber}) 内にコマンドがありません");

            var ifCommand = CreateIfCommandInstance(parent, ifItem);
            ifCommand.Children = ListItemToCommand(ifCommand, childrenListItems);
            
            // 子要素のフラグを設定
            foreach (var child in childrenListItems.Where(x => x.NestLevel == ifItem.NestLevel + 1))
            {
                child.IsInIf = true;
            }
            
            return ifCommand;
        }

        private static IIfCommand CreateIfCommandInstance(ICommand parent, IIfItem ifItem)
        {
            Debug.WriteLine($"Creating If command instance for {ifItem.GetType().Name}");
            
            switch (ifItem)
            {
                case IfImageExistItem exist:
                    Debug.WriteLine($"Creating IfImageExistCommand");
                    return new IfImageExistCommand(parent, new IfImageCommandSettings 
                    { 
                        ImagePath = exist.ImagePath,
                        Threshold = exist.Threshold,
                        SearchColor = exist.SearchColor,
                        WindowTitle = exist.WindowTitle,
                        WindowClassName = exist.WindowClassName
                    }) 
                    { 
                        LineNumber = exist.LineNumber, 
                        IsEnabled = exist.IsEnable 
                    };
                    
                case IfImageNotExistItem notExist:
                    Debug.WriteLine($"Creating IfImageNotExistCommand");
                    return new IfImageNotExistCommand(parent, new IfImageCommandSettings 
                    { 
                        ImagePath = notExist.ImagePath,
                        Threshold = notExist.Threshold,
                        SearchColor = notExist.SearchColor,
                        WindowTitle = notExist.WindowTitle,
                        WindowClassName = notExist.WindowClassName
                    }) 
                    { 
                        LineNumber = notExist.LineNumber, 
                        IsEnabled = notExist.IsEnable 
                    };
                    
                case IfImageExistAIItem existAI:
                    Debug.WriteLine($"Creating IfImageExistAICommand");
                    return new IfImageExistAICommand(parent, new AIImageDetectCommandSettings 
                    { 
                        ModelPath = existAI.ModelPath,
                        ClassID = existAI.ClassID,
                        ConfThreshold = existAI.ConfThreshold,
                        IoUThreshold = existAI.IoUThreshold,
                        WindowTitle = existAI.WindowTitle,
                        WindowClassName = existAI.WindowClassName
                    }) 
                    { 
                        LineNumber = existAI.LineNumber, 
                        IsEnabled = existAI.IsEnable 
                    };
                    
                case IfImageNotExistAIItem notExistAI:
                    Debug.WriteLine($"Creating IfImageNotExistAICommand");
                    return new IfImageNotExistAICommand(parent, new AIImageNotDetectCommandSettings 
                    { 
                        ModelPath = notExistAI.ModelPath,
                        ClassID = notExistAI.ClassID,
                        ConfThreshold = notExistAI.ConfThreshold,
                        IoUThreshold = notExistAI.IoUThreshold,
                        WindowTitle = notExistAI.WindowTitle,
                        WindowClassName = notExistAI.WindowClassName
                    }) 
                    { 
                        LineNumber = notExistAI.LineNumber, 
                        IsEnabled = notExistAI.IsEnable 
                    };
                    
                case IfVariableItem ifVar:
                    Debug.WriteLine($"Creating IfVariableCommand");
                    return new IfVariableCommand(parent, new IfVariableCommandSettings 
                    { 
                        Name = ifVar.Name,
                        Operator = ifVar.Operator,
                        Value = ifVar.Value
                    }) 
                    { 
                        LineNumber = ifVar.LineNumber, 
                        IsEnabled = ifVar.IsEnable 
                    };
                    
                default:
                    throw new NotSupportedException($"未対応のIf文型です: {ifItem.GetType().Name}");
            }
        }

        private static LoopCommand CreateLoopCommand(ICommand parent, ILoopItem loopItem, IEnumerable<ICommandListItem> items)
        {
            if (loopItem.Pair == null)
                throw new InvalidOperationException($"ループ (行 {loopItem.LineNumber}) に対応するEndLoopがありません");

            var endLoopItem = loopItem.Pair;
            var childrenListItems = items
                .Where(x => x.LineNumber > loopItem.LineNumber && x.LineNumber < endLoopItem.LineNumber)
                .ToList();

            if (childrenListItems.Count == 0)
                throw new InvalidOperationException($"ループ (行 {loopItem.LineNumber}) 内にコマンドがありません");

            var endLoopCommand = parent.Children.FirstOrDefault(x => x.LineNumber == endLoopItem.LineNumber) as LoopEndCommand;
            
            // LoopItemから直接設定を作成
            var loopCommand = new LoopCommand(parent, new LoopCommandSettings() 
            { 
                LoopCount = loopItem.LoopCount, 
                Pair = endLoopCommand 
            })
            {
                LineNumber = loopItem.LineNumber,
                IsEnabled = loopItem.IsEnable,
            };

            loopCommand.Children = ListItemToCommand(loopCommand, childrenListItems);
            
            // 子要素のフラグを設定
            foreach (var child in childrenListItems.Where(x => x.NestLevel == loopItem.NestLevel + 1))
            {
                child.IsInLoop = true;
            }
            
            Debug.WriteLine($"Successfully created LoopCommand with {childrenListItems.Count} children");
            
            return loopCommand;
        }
    }
}
