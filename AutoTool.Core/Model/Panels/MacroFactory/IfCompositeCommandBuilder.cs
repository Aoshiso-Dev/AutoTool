using AutoTool.Commands.Commands;
using AutoTool.Commands.DependencyInjection;
using AutoTool.Commands.Interface;
using AutoTool.Panels.List.Class;
using AutoTool.Panels.Model.List.Interface;

namespace AutoTool.Panels.Model.MacroFactory;

public sealed class IfCompositeCommandBuilder : ICompositeCommandBuilder
{
    private readonly ICommandFactory _commandFactory;

    public IfCompositeCommandBuilder(ICommandFactory commandFactory)
    {
        _commandFactory = commandFactory ?? throw new ArgumentNullException(nameof(commandFactory));
    }

    public bool CanBuild(ICommandListItem item) => item is IIfItem;

    public ICommand Build(
        ICommand parent,
        ICommandListItem item,
        IEnumerable<ICommandListItem> items,
        Func<ICommand, IEnumerable<ICommandListItem>, IEnumerable<ICommand>> buildChildren)
    {
        var ifItem = (IIfItem)item;
        if (ifItem.Pair == null)
        {
            throw new PairMismatchException($"If文 (行 {ifItem.LineNumber}) に対応するEndIfがありません", ifItem.LineNumber, ifItem.ItemType);
        }

        var endIfItem = ifItem.Pair;
        var childrenListItems = items
            .Where(x => x.LineNumber > ifItem.LineNumber && x.LineNumber < endIfItem.LineNumber)
            .ToList();

        if (childrenListItems.Count == 0)
        {
            throw new EmptyStructureException($"If文 (行 {ifItem.LineNumber}) 内にコマンドがありません", ifItem.LineNumber, ifItem.ItemType);
        }

        var ifCommand = CreateIfCommandInstance(parent, ifItem);
        ifCommand.Children = buildChildren(ifCommand, childrenListItems);

        return ifCommand;
    }

    private IIfCommand CreateIfCommandInstance(ICommand parent, IIfItem ifItem)
    {
        IIfCommand command = ifItem switch
        {
            IfImageExistItem exist => _commandFactory.Create<IfImageExistCommand>(
                parent,
                new IfImageCommandSettings
                {
                    ImagePath = exist.ImagePath,
                    Threshold = exist.Threshold,
                    SearchColor = exist.SearchColor,
                    WindowTitle = exist.WindowTitle,
                    WindowClassName = exist.WindowClassName
                }),
            IfImageNotExistItem notExist => _commandFactory.Create<IfImageNotExistCommand>(
                parent,
                new IfImageCommandSettings
                {
                    ImagePath = notExist.ImagePath,
                    Threshold = notExist.Threshold,
                    SearchColor = notExist.SearchColor,
                    WindowTitle = notExist.WindowTitle,
                    WindowClassName = notExist.WindowClassName
                }),
            IfTextExistItem textExist => _commandFactory.Create<IfTextExistCommand>(
                parent,
                new IfTextCommandSettings
                {
                    TargetText = textExist.TargetText,
                    CaseSensitive = textExist.CaseSensitive,
                    MatchMode = textExist.MatchMode,
                    X = textExist.X,
                    Y = textExist.Y,
                    Width = textExist.Width,
                    Height = textExist.Height,
                    MinConfidence = textExist.MinConfidence,
                    WindowTitle = textExist.WindowTitle,
                    WindowClassName = textExist.WindowClassName,
                    Language = textExist.Language,
                    PageSegmentationMode = textExist.PageSegmentationMode,
                    Whitelist = textExist.Whitelist,
                    PreprocessMode = textExist.PreprocessMode,
                    TessdataPath = textExist.TessdataPath
                }),
            IfTextNotExistItem textNotExist => _commandFactory.Create<IfTextNotExistCommand>(
                parent,
                new IfTextCommandSettings
                {
                    TargetText = textNotExist.TargetText,
                    CaseSensitive = textNotExist.CaseSensitive,
                    MatchMode = textNotExist.MatchMode,
                    X = textNotExist.X,
                    Y = textNotExist.Y,
                    Width = textNotExist.Width,
                    Height = textNotExist.Height,
                    MinConfidence = textNotExist.MinConfidence,
                    WindowTitle = textNotExist.WindowTitle,
                    WindowClassName = textNotExist.WindowClassName,
                    Language = textNotExist.Language,
                    PageSegmentationMode = textNotExist.PageSegmentationMode,
                    Whitelist = textNotExist.Whitelist,
                    PreprocessMode = textNotExist.PreprocessMode,
                    TessdataPath = textNotExist.TessdataPath
                }),
            IfImageExistAIItem existAI => _commandFactory.Create<IfImageExistAICommand>(
                parent,
                new AIImageDetectCommandSettings
                {
                    ModelPath = existAI.ModelPath,
                    ClassID = existAI.ClassID,
                    ConfThreshold = existAI.ConfThreshold,
                    IoUThreshold = existAI.IoUThreshold,
                    WindowTitle = existAI.WindowTitle,
                    WindowClassName = existAI.WindowClassName
                }),
            IfImageNotExistAIItem notExistAI => _commandFactory.Create<IfImageNotExistAICommand>(
                parent,
                new AIImageNotDetectCommandSettings
                {
                    ModelPath = notExistAI.ModelPath,
                    ClassID = notExistAI.ClassID,
                    ConfThreshold = notExistAI.ConfThreshold,
                    IoUThreshold = notExistAI.IoUThreshold,
                    WindowTitle = notExistAI.WindowTitle,
                    WindowClassName = notExistAI.WindowClassName
                }),
            IfVariableItem ifVar => _commandFactory.Create<IfVariableCommand>(
                parent,
                new IfVariableCommandSettings
                {
                    Name = ifVar.Name,
                    Operator = ifVar.Operator,
                    Value = ifVar.Value
                }),
            _ => throw new UnsupportedCommandTypeException($"未対応のIf文型です: {ifItem.GetType().Name}", ifItem.LineNumber, ifItem.ItemType)
        };

        command.LineNumber = ifItem.LineNumber;
        command.IsEnabled = ifItem.IsEnable;
        return command;
    }
}
