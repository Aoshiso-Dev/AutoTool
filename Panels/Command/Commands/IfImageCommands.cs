using MacroPanels.Command.Interface;
using MacroPanels.Command.Services;

namespace MacroPanels.Command.Commands;

/// <summary>
/// If基本コマンド
/// </summary>
public class IfCommand : BaseCommand, IIfCommand
{
    public IfCommand(ICommand? parent, ICommandSettings settings) : base(parent, settings) { }

    protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }
}

/// <summary>
/// If終了コマンド
/// </summary>
public class IfEndCommand : BaseCommand
{
    public IfEndCommand(ICommand? parent, ICommandSettings settings) : base(parent, settings) { }

    protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        ResetChildrenProgress();
        return Task.FromResult(true);
    }
}

/// <summary>
/// 画像存在判定コマンド
/// </summary>
public class IfImageExistCommand : BaseCommand, IIfCommand, IIfImageExistCommand
{
    private readonly IImageSearchService _imageSearchService;

    public new IIfImageCommandSettings Settings => (IIfImageCommandSettings)base.Settings;

    public IfImageExistCommand(ICommand? parent, ICommandSettings settings, IImageSearchService imageSearchService)
        : base(parent, settings)
    {
        _imageSearchService = imageSearchService ?? throw new ArgumentNullException(nameof(imageSearchService));
    }

    protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        if (Children == null || !Children.Any())
        {
            throw new InvalidOperationException("If内に要素がありません。");
        }

        var point = await _imageSearchService.SearchImageAsync(
            Settings.ImagePath,
            cancellationToken,
            Settings.Threshold,
            Settings.SearchColor,
            Settings.WindowTitle,
            Settings.WindowClassName);

        if (point != null)
        {
            RaiseDoingCommand($"画像が見つかりました。({point.Value.X}, {point.Value.Y})");
            return await ExecuteChildrenAsync(cancellationToken);
        }

        RaiseDoingCommand("画像が見つかりませんでした。");
        return true;
    }
}

/// <summary>
/// 画像非存在判定コマンド
/// </summary>
public class IfImageNotExistCommand : BaseCommand, IIfCommand, IIfImageNotExistCommand
{
    private readonly IImageSearchService _imageSearchService;

    public new IIfImageCommandSettings Settings => (IIfImageCommandSettings)base.Settings;

    public IfImageNotExistCommand(ICommand? parent, ICommandSettings settings, IImageSearchService imageSearchService)
        : base(parent, settings)
    {
        _imageSearchService = imageSearchService ?? throw new ArgumentNullException(nameof(imageSearchService));
    }

    protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        if (Children == null || !Children.Any())
        {
            throw new InvalidOperationException("If内に要素がありません。");
        }

        var point = await _imageSearchService.SearchImageAsync(
            Settings.ImagePath,
            cancellationToken,
            Settings.Threshold,
            Settings.SearchColor,
            Settings.WindowTitle,
            Settings.WindowClassName);

        if (point == null)
        {
            RaiseDoingCommand("画像が見つかりませんでした。");
            return await ExecuteChildrenAsync(cancellationToken);
        }

        RaiseDoingCommand($"画像が見つかりました。({point.Value.X}, {point.Value.Y})");
        return true;
    }
}

/// <summary>
/// 変数条件判定コマンド
/// </summary>
public class IfVariableCommand : BaseCommand, IIfCommand
{
    private readonly IVariableStore _variableStore;

    public new IIfVariableCommandSettings Settings => (IIfVariableCommandSettings)base.Settings;

    public IfVariableCommand(ICommand? parent, ICommandSettings settings, IVariableStore variableStore)
        : base(parent, settings)
    {
        _variableStore = variableStore ?? throw new ArgumentNullException(nameof(variableStore));
    }

    protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        if (Children == null || !Children.Any())
        {
            throw new InvalidOperationException("If文中に要素がありません。");
        }

        var actualValue = _variableStore.Get(Settings.Name) ?? "";
        var expectedValue = Settings.Value;
        
        bool conditionMet = Settings.Operator switch
        {
            "==" => actualValue == expectedValue,
            "!=" => actualValue != expectedValue,
            ">" => double.TryParse(actualValue, out var a) && double.TryParse(expectedValue, out var b) && a > b,
            "<" => double.TryParse(actualValue, out var a2) && double.TryParse(expectedValue, out var b2) && a2 < b2,
            ">=" => double.TryParse(actualValue, out var a3) && double.TryParse(expectedValue, out var b3) && a3 >= b3,
            "<=" => double.TryParse(actualValue, out var a4) && double.TryParse(expectedValue, out var b4) && a4 <= b4,
            _ => false
        };

        if (conditionMet)
        {
            RaiseDoingCommand($"条件一致: {Settings.Name} {Settings.Operator} \"{expectedValue}\" (実際: \"{actualValue}\")");
            return await ExecuteChildrenAsync(cancellationToken);
        }

        RaiseDoingCommand($"条件不一致: {Settings.Name} {Settings.Operator} \"{expectedValue}\" (実際: \"{actualValue}\")");
        return true;
    }
}
