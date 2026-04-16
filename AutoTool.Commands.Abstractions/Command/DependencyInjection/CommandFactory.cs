using System.Reflection;
using AutoTool.Commands.Commands;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Services;

namespace AutoTool.Commands.DependencyInjection;

/// <summary>
/// DI コンテナを利用したコマンドファクトリ
/// </summary>
public class CommandFactory : ICommandFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICommandEventBus? _commandEventBus;

    public CommandFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _commandEventBus = _serviceProvider.GetService(typeof(ICommandEventBus)) as ICommandEventBus;
    }

    public TCommand Create<TCommand>(ICommand? parent, ICommandSettings settings) where TCommand : ICommand
    {
        return (TCommand)Create(typeof(TCommand), parent, settings);
    }

    public ICommand Create(Type commandType, ICommand? parent, ICommandSettings settings)
    {
        return Create(commandType, parent, settings, []);
    }

    public ICommand Create(Type commandType, ICommand? parent, ICommandSettings settings, params object[] explicitArguments)
    {
        ArgumentNullException.ThrowIfNull(commandType);
        explicitArguments ??= [];

        if (commandType == typeof(RootCommand))
        {
            return AttachEventBus(new RootCommand());
        }

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

                if (paramType == typeof(ICommand) || paramType.IsAssignableTo(typeof(ICommand)))
                {
                    args[i] = parent;
                }
                else if (paramType == typeof(ICommandSettings) || paramType.IsAssignableTo(typeof(ICommandSettings)))
                {
                    args[i] = settings;
                }
                else if (TryResolveExplicitArgument(explicitArguments, paramType, out var explicitArg))
                {
                    args[i] = explicitArg;
                }
                else
                {
                    var service = _serviceProvider.GetService(paramType);
                    if (service is not null)
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
                var command = (ICommand)constructor.Invoke(args);
                return AttachEventBus(command);
            }
        }

        throw new InvalidOperationException(
            $"コマンド {commandType.Name} を生成できませんでした。必要なコンストラクタ引数が解決できていません。");
    }

    private static bool TryResolveExplicitArgument(IEnumerable<object> explicitArguments, Type paramType, out object? argument)
    {
        foreach (var candidate in explicitArguments)
        {
            if (candidate is null)
            {
                continue;
            }

            if (paramType.IsInstanceOfType(candidate))
            {
                argument = candidate;
                return true;
            }
        }

        argument = null;
        return false;
    }

    private ICommand AttachEventBus(ICommand command)
    {
        if (command is BaseCommand baseCommand && _commandEventBus is not null)
        {
            baseCommand.SetEventBus(_commandEventBus);
        }

        return command;
    }
}

/// <summary>
/// コマンドファクトリのインターフェース
/// </summary>
public interface ICommandFactory
{
    TCommand Create<TCommand>(ICommand? parent, ICommandSettings settings) where TCommand : ICommand;

    ICommand Create(Type commandType, ICommand? parent, ICommandSettings settings);

    ICommand Create(Type commandType, ICommand? parent, ICommandSettings settings, params object[] explicitArguments);
}
