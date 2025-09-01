using MacroPanels.Command.Interface;
using MacroPanels.Command.Class;
using MacroPanels.List.Class;
using System.Diagnostics;
using MacroPanels.Model.List.Interface;
using MacroPanels.Model.CommandDefinition;
using MouseHelper;

namespace MacroPanels.Model.MacroFactory
{
    public static class MacroFactory
    {
        // If命令の設定作成デリゲートのディクショナリ
        private static readonly Dictionary<Type, Func<IIfItem, ICommandSettings>> _ifSettingsCreators = 
            new Dictionary<Type, Func<IIfItem, ICommandSettings>>
            {
                { typeof(IfImageExistItem), item => CreateIfImageSettings((IfImageExistItem)item) },
                { typeof(IfImageNotExistItem), item => CreateIfImageSettings((IfImageNotExistItem)item) },
                { typeof(IfImageExistAIItem), item => CreateIfImageExistAISettings((IfImageExistAIItem)item) },
                { typeof(IfImageNotExistAIItem), item => CreateIfImageNotExistAISettings((IfImageNotExistAIItem)item) },
                { typeof(IfVariableItem), item => CreateIfVariableSettings((IfVariableItem)item) }
            };

        // If命令のコマンドクラスマッピング
        private static readonly Dictionary<Type, Type> _ifCommandTypes = new Dictionary<Type, Type>
        {
            { typeof(IfImageExistItem), typeof(IfImageExistCommand) },
            { typeof(IfImageNotExistItem), typeof(IfImageNotExistCommand) },
            { typeof(IfImageExistAIItem), typeof(IfImageExistAICommand) },
            { typeof(IfImageNotExistAIItem), typeof(IfImageNotExistAICommand) },
            { typeof(IfVariableItem), typeof(IfVariableCommand) }
        };

        /// <summary>
        /// MouseButton操作の共通実行メソッド
        /// </summary>
        public static async Task ExecuteMouseButtonAction(System.Windows.Input.MouseButton button, int x, int y, string windowTitle, string windowClassName)
        {
            switch (button)
            {
                case System.Windows.Input.MouseButton.Left:
                    await MouseHelper.Input.ClickAsync(x, y, windowTitle, windowClassName);
                    break;
                case System.Windows.Input.MouseButton.Right:
                    await MouseHelper.Input.RightClickAsync(x, y, windowTitle, windowClassName);
                    break;
                case System.Windows.Input.MouseButton.Middle:
                    await MouseHelper.Input.MiddleClickAsync(x, y, windowTitle, windowClassName);
                    break;
                default:
                    throw new Exception("マウスボタンが不正です。");
            }
        }

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
            return item switch
            {
                IIfItem ifItem => CreateIfCommand(parent, ifItem, items),
                ILoopItem loopItem => CreateLoopCommand(parent, loopItem, items),
                _ => throw new NotSupportedException($"未対応のアイテム型です: {item.GetType().Name} (ItemType: {item.ItemType})")
            };
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
            
            var itemType = ifItem.GetType();
            
            // 設定オブジェクト作成
            if (!_ifSettingsCreators.TryGetValue(itemType, out var settingsCreator))
                throw new NotSupportedException($"未対応のIf文型です: {itemType.Name}");
            
            // コマンドクラス取得
            if (!_ifCommandTypes.TryGetValue(itemType, out var commandType))
                throw new NotSupportedException($"未対応のIf文型です: {itemType.Name}");
            
            var settings = settingsCreator(ifItem);
            var command = (IIfCommand)Activator.CreateInstance(commandType, parent, settings)!;
            
            command.LineNumber = ifItem.LineNumber;
            command.IsEnabled = ifItem.IsEnable;
            
            Debug.WriteLine($"Successfully created {commandType.Name}");
            return command;
        }

        // 個別の設定作成メソッド
        private static ICommandSettings CreateIfImageSettings(IfImageExistItem item) =>
            new IfImageCommandSettings 
            { 
                ImagePath = item.ImagePath,
                Threshold = item.Threshold,
                SearchColor = item.SearchColor,
                WindowTitle = item.WindowTitle,
                WindowClassName = item.WindowClassName
            };

        private static ICommandSettings CreateIfImageSettings(IfImageNotExistItem item) =>
            new IfImageCommandSettings 
            { 
                ImagePath = item.ImagePath,
                Threshold = item.Threshold,
                SearchColor = item.SearchColor,
                WindowTitle = item.WindowTitle,
                WindowClassName = item.WindowClassName
            };

        private static ICommandSettings CreateIfImageExistAISettings(IfImageExistAIItem item) =>
            new AIImageDetectCommandSettings 
            { 
                ModelPath = item.ModelPath,
                ClassID = item.ClassID,
                ConfThreshold = item.ConfThreshold,
                IoUThreshold = item.IoUThreshold,
                WindowTitle = item.WindowTitle,
                WindowClassName = item.WindowClassName
            };

        private static ICommandSettings CreateIfImageNotExistAISettings(IfImageNotExistAIItem item) =>
            new AIImageNotDetectCommandSettings 
            { 
                ModelPath = item.ModelPath,
                ClassID = item.ClassID,
                ConfThreshold = item.ConfThreshold,
                IoUThreshold = item.IoUThreshold,
                WindowTitle = item.WindowTitle,
                WindowClassName = item.WindowClassName
            };

        private static ICommandSettings CreateIfVariableSettings(IfVariableItem item) =>
            new IfVariableCommandSettings 
            { 
                Name = item.Name,
                Operator = item.Operator,
                Value = item.Value
            };

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
