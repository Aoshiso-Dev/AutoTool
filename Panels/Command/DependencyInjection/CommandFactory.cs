using System.Reflection;
using MacroPanels.Command.Commands;
using MacroPanels.Command.Interface;

namespace MacroPanels.Command.DependencyInjection;

/// <summary>
/// DIコンテナを使用したコマンドファクトリ
/// </summary>
public class CommandFactory : ICommandFactory
{
    private readonly IServiceProvider _serviceProvider;

    public CommandFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// 指定された型のコマンドを作成します
    /// </summary>
    public TCommand Create<TCommand>(ICommand? parent, ICommandSettings settings) where TCommand : ICommand
    {
        return (TCommand)Create(typeof(TCommand), parent, settings);
    }

    /// <summary>
    /// 指定された型のコマンドを作成します
    /// </summary>
    public ICommand Create(Type commandType, ICommand? parent, ICommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(commandType);

        // 特殊なケース: RootCommandはパラメータなし
        if (commandType == typeof(RootCommand))
        {
            return new RootCommand();
        }

        // コンストラクタを取得（パラメータ数の多い順にソート）
        var constructors = commandType.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .ToList();

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            var args = new object?[parameters.Length];
            var allResolved = true;

            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;

                // 親コマンド
                if (paramType == typeof(ICommand) || paramType.IsAssignableTo(typeof(ICommand)))
                {
                    args[i] = parent;
                }
                // 設定
                else if (paramType == typeof(ICommandSettings) || paramType.IsAssignableTo(typeof(ICommandSettings)))
                {
                    args[i] = settings;
                }
                // サービスを解決
                else
                {
                    var service = _serviceProvider.GetService(paramType);
                    if (service != null)
                    {
                        args[i] = service;
                    }
                    else
                    {
                        allResolved = false;
                        break;
                    }
                }
            }

            if (allResolved)
            {
                return (ICommand)constructor.Invoke(args);
            }
        }

        throw new InvalidOperationException(
            $"コマンド {commandType.Name} を作成できませんでした。適切なコンストラクタが見つかりません。");
    }
}

/// <summary>
/// コマンドファクトリのインターフェース
/// </summary>
public interface ICommandFactory
{
    /// <summary>
    /// 指定された型のコマンドを作成します
    /// </summary>
    TCommand Create<TCommand>(ICommand? parent, ICommandSettings settings) where TCommand : ICommand;

    /// <summary>
    /// 指定された型のコマンドを作成します
    /// </summary>
    ICommand Create(Type commandType, ICommand? parent, ICommandSettings settings);
}
