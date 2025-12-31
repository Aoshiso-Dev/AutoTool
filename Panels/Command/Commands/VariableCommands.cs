using MacroPanels.Command.Interface;
using MacroPanels.Command.Services;

namespace MacroPanels.Command.Commands;

/// <summary>
/// 変数設定コマンド
/// </summary>
public class SetVariableCommand : BaseCommand, ISetVariableCommand
{
    private readonly IVariableStore _variableStore;

    public new ISetVariableCommandSettings Settings => (ISetVariableCommandSettings)base.Settings;

    public SetVariableCommand(ICommand? parent, ICommandSettings settings, IVariableStore variableStore)
        : base(parent, settings)
    {
        _variableStore = variableStore ?? throw new ArgumentNullException(nameof(variableStore));
    }

    protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        _variableStore.Set(Settings.Name, Settings.Value);
        RaiseDoingCommand($"変数を設定しました。{Settings.Name} = \"{Settings.Value}\"");
        return Task.FromResult(true);
    }
}

/// <summary>
/// AI検出結果を変数に設定するコマンド
/// </summary>
public class SetVariableAICommand : BaseCommand, ISetVariableAICommand
{
    private readonly IAIDetectionService _aiDetectionService;
    private readonly IVariableStore _variableStore;

    public new ISetVariableAICommandSettings Settings => (ISetVariableAICommandSettings)base.Settings;

    public SetVariableAICommand(
        ICommand? parent,
        ICommandSettings settings,
        IAIDetectionService aiDetectionService,
        IVariableStore variableStore)
        : base(parent, settings)
    {
        _aiDetectionService = aiDetectionService ?? throw new ArgumentNullException(nameof(aiDetectionService));
        _variableStore = variableStore ?? throw new ArgumentNullException(nameof(variableStore));
    }

    protected override Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
    {
        _aiDetectionService.Initialize(Settings.ModelPath, 640, true);

        var detections = _aiDetectionService.Detect(
            Settings.WindowTitle,
            (float)Settings.ConfThreshold,
            (float)Settings.IoUThreshold);

        if (detections.Count == 0)
        {
            _variableStore.Set(Settings.Name, "-1");
            RaiseDoingCommand($"画像が見つかりませんでした。{Settings.Name}に-1をセットしました。");
        }
        else
        {
            switch (Settings.AIDetectMode)
            {
                case "Class":
                    var best = detections.OrderByDescending(d => d.Score).First();
                    _variableStore.Set(Settings.Name, best.ClassId.ToString());
                    RaiseDoingCommand($"画像が見つかりました。{Settings.Name}に{best.ClassId}をセットしました。");
                    break;
                case "Count":
                    _variableStore.Set(Settings.Name, detections.Count.ToString());
                    RaiseDoingCommand($"画像が{detections.Count}個見つかりました。{Settings.Name}に{detections.Count}をセットしました。");
                    break;
                default:
                    throw new InvalidOperationException($"不明なモードです: {Settings.AIDetectMode}");
            }
        }

        return Task.FromResult(true);
    }
}

/// <summary>
/// 変数条件判定コマンド
/// </summary>
public class IfVariableCommand : BaseCommand, IIfVariableCommand
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
            throw new InvalidOperationException("If内に要素がありません。");
        }

        var lhs = _variableStore.Get(Settings.Name) ?? string.Empty;
        var rhs = Settings.Value ?? string.Empty;

        bool result = Evaluate(lhs, rhs, Settings.Operator);
        RaiseDoingCommand($"IfVariable: {Settings.Name}({lhs}) {Settings.Operator} {rhs} => {result}");

        if (result)
        {
            return await ExecuteChildrenAsync(cancellationToken);
        }

        return true;
    }

    private static bool Evaluate(string lhs, string rhs, string op)
    {
        op = (op ?? "").Trim();

        if (double.TryParse(lhs, out var lnum) && double.TryParse(rhs, out var rnum))
        {
            return op switch
            {
                "==" => lnum == rnum,
                "!=" => lnum != rnum,
                ">" => lnum > rnum,
                "<" => lnum < rnum,
                ">=" => lnum >= rnum,
                "<=" => lnum <= rnum,
                _ => throw new InvalidOperationException($"不明な数値比較演算子です: {op}"),
            };
        }
        else
        {
            return op switch
            {
                "==" => string.Equals(lhs, rhs, StringComparison.Ordinal),
                "!=" => !string.Equals(lhs, rhs, StringComparison.Ordinal),
                "Contains" => lhs.Contains(rhs, StringComparison.Ordinal),
                "StartsWith" => lhs.StartsWith(rhs, StringComparison.Ordinal),
                "EndsWith" => lhs.EndsWith(rhs, StringComparison.Ordinal),
                "IsEmpty" => string.IsNullOrEmpty(lhs),
                "IsNotEmpty" => !string.IsNullOrEmpty(lhs),
                _ => throw new InvalidOperationException($"不明な文字列比較演算子です: {op}"),
            };
        }
    }
}
