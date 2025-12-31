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
