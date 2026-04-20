using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using AutoTool.Commands.Commands;
using AutoTool.Commands.Interface;
using AutoTool.Commands.Services;

namespace AutoTool.Commands.DependencyInjection;

/// <summary>
/// DI コンテナを利用したコマンドファクトリ
/// </summary>
public class CommandFactory(ICommandDependencyResolver dependencyResolver, ICommandEventBus? commandEventBus = null) : ICommandFactory
{
    /// <summary>
    /// この機能で扱う状態や種別の選択肢を列挙し、分岐条件を明確にします。
    /// </summary>

    private enum ParameterSource
    {
        ParentCommand,
        CommandSettings,
        ExplicitOrService
    }

    /// <summary>
    /// コンストラクタ引数ごとの解決方針を保持し、実行時の依存解決手順を再利用できるようにします。
    /// </summary>

    private sealed class ParameterPlan
    {
        public required Type ParameterType { get; init; }
        public required ParameterSource Source { get; init; }
        public required bool HasDefaultValue { get; init; }
        public object? DefaultValue { get; init; }
    }

    /// <summary>
    /// 選択したコンストラクタと引数解決計画を保持し、インスタンス生成処理で利用できるようにします。
    /// </summary>

    private sealed class ConstructorPlan
    {
        public required ConstructorInfo Constructor { get; init; }
        public required IReadOnlyList<ParameterPlan> Parameters { get; init; }
        public required Func<object?[], ICommand> Activator { get; init; }
    }

    /// <summary>
    /// 実行時に必要な型や依存関係を解決し、利用可能なインスタンスを返します。
    /// </summary>

    private sealed class ExplicitArgumentResolver
    {
        private readonly IReadOnlyList<object> _arguments;
        private readonly Dictionary<Type, object?> _cache = new();

        public ExplicitArgumentResolver(IEnumerable<object> explicitArguments)
        {
            _arguments = explicitArguments
                .Where(static x => x is not null)
                .ToArray();
        }

        public bool TryResolve(Type parameterType, out object? argument)
        {
            if (_cache.TryGetValue(parameterType, out argument))
            {
                return argument is not null;
            }

            foreach (var candidate in _arguments)
            {
                if (parameterType.IsInstanceOfType(candidate))
                {
                    _cache[parameterType] = candidate;
                    argument = candidate;
                    return true;
                }
            }

            _cache[parameterType] = null;
            argument = null;
            return false;
        }
    }

    private static readonly ConcurrentDictionary<Type, IReadOnlyList<ConstructorPlan>> ConstructorPlanCache = new();
    private readonly ICommandDependencyResolver _dependencyResolver = EnsureNotNull(dependencyResolver);
    private readonly ICommandEventBus? _commandEventBus = commandEventBus;

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
        ArgumentNullException.ThrowIfNull(settings);
        explicitArguments ??= [];

        if (commandType == typeof(RootCommand))
        {
            return AttachEventBus(new RootCommand());
        }

        var constructorPlans = ConstructorPlanCache.GetOrAdd(commandType, BuildConstructorPlans);
        var explicitArgumentResolver = new ExplicitArgumentResolver(explicitArguments);
        Dictionary<Type, object?> resolvedServices = [];
        foreach (var constructorPlan in constructorPlans)
        {
            var args = new object?[constructorPlan.Parameters.Count];
            var allResolved = true;

            for (var i = 0; i < constructorPlan.Parameters.Count; i++)
            {
                var parameterPlan = constructorPlan.Parameters[i];
                switch (parameterPlan.Source)
                {
                    case ParameterSource.ParentCommand:
                        args[i] = parent;
                        break;
                    case ParameterSource.CommandSettings:
                        args[i] = settings;
                        break;
                    case ParameterSource.ExplicitOrService:
                        if (explicitArgumentResolver.TryResolve(parameterPlan.ParameterType, out var explicitArg))
                        {
                            args[i] = explicitArg;
                            break;
                        }

                        if (TryResolveService(parameterPlan, resolvedServices, out var service))
                        {
                            args[i] = service;
                            break;
                        }

                        if (parameterPlan.HasDefaultValue)
                        {
                            args[i] = parameterPlan.DefaultValue;
                            break;
                        }

                        allResolved = false;
                        break;
                    default:
                        allResolved = false;
                        break;
                }

                if (!allResolved)
                {
                    break;
                }
            }

            if (!allResolved)
            {
                continue;
            }

            var command = constructorPlan.Activator(args);
            return AttachEventBus(command);
        }

        throw new InvalidOperationException(
            $"コマンド {commandType.Name} を生成できませんでした。必要なコンストラクタ引数が解決できていません。");
    }

    private bool TryResolveService(
        ParameterPlan parameterPlan,
        IDictionary<Type, object?> resolvedServices,
        out object? service)
    {
        if (resolvedServices.TryGetValue(parameterPlan.ParameterType, out service))
        {
            return service is not null;
        }

        service = _dependencyResolver.TryResolve(parameterPlan.ParameterType, out var resolved)
            ? resolved
            : null;
        resolvedServices[parameterPlan.ParameterType] = service;
        return service is not null;
    }

    private static IReadOnlyList<ConstructorPlan> BuildConstructorPlans(Type commandType)
    {
        return commandType
            .GetConstructors()
            .OrderByDescending(static c => c.GetParameters().Length)
            .Select(static constructor => new ConstructorPlan
            {
                Constructor = constructor,
                Parameters = constructor
                    .GetParameters()
                    .Select(static p => new ParameterPlan
                    {
                        ParameterType = p.ParameterType,
                        Source = ResolveParameterSource(p.ParameterType),
                        HasDefaultValue = p.HasDefaultValue,
                        DefaultValue = p.DefaultValue
                    })
                    .ToArray(),
                Activator = CreateActivator(constructor)
            })
            .ToArray();
    }

    private static Func<object?[], ICommand> CreateActivator(ConstructorInfo constructor)
    {
        var argsParam = Expression.Parameter(typeof(object[]), "args");
        var parameters = constructor.GetParameters();

        var arguments = parameters
            .Select((p, i) => Expression.Convert(
                Expression.ArrayIndex(argsParam, Expression.Constant(i)),
                p.ParameterType))
            .ToArray();

        var body = Expression.Convert(Expression.New(constructor, arguments), typeof(ICommand));
        return Expression.Lambda<Func<object?[], ICommand>>(body, argsParam).Compile();
    }

    private static ParameterSource ResolveParameterSource(Type parameterType)
    {
        if (parameterType == typeof(ICommand) || parameterType.IsAssignableTo(typeof(ICommand)))
        {
            return ParameterSource.ParentCommand;
        }

        if (parameterType == typeof(ICommandSettings) || parameterType.IsAssignableTo(typeof(ICommandSettings)))
        {
            return ParameterSource.CommandSettings;
        }

        return ParameterSource.ExplicitOrService;
    }

    private ICommand AttachEventBus(ICommand command)
    {
        if (command is BaseCommand baseCommand && _commandEventBus is not null)
        {
            baseCommand.SetEventBus(_commandEventBus);
        }

        return command;
    }

    private static ICommandDependencyResolver EnsureNotNull(ICommandDependencyResolver value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value;
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
