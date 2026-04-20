using AutoTool.Commands.Services;

namespace AutoTool.Commands.DependencyInjection;

/// <summary>
/// コマンド依存解決の抽象
/// </summary>
public interface ICommandDependencyResolver
{
    bool TryResolve(Type serviceType, out object? service);
}

/// <summary>
/// コマンド実行に必要な依存を用途別 Resolver で構成して解決する実装
/// </summary>
public sealed class CommandDependencyResolver(
    IVariableStore variableStore,
    IObjectDetector objectDetector,
    IPathResolver pathResolver,
    IImageMatcher imageMatcher,
    IMouseInput mouseInput,
    IKeyboardInput keyboardInput,
    IScreenCapturer screenCapturer,
    IProcessLauncher processLauncher,
    IWindowService windowService,
    IOcrEngine ocrEngine,
    ICommandEventBus? commandEventBus = null,
    TimeProvider? timeProvider = null) : ICommandDependencyResolver
{
    private readonly CompositeCommandDependencyResolver _composite = new(
    [
        new CoreCommandDependencyResolver(
            variableStore,
            objectDetector,
            pathResolver,
            imageMatcher,
            mouseInput,
            keyboardInput,
            screenCapturer,
            processLauncher,
            windowService,
            ocrEngine),
        new AmbientCommandDependencyResolver(
            commandEventBus,
            timeProvider ?? TimeProvider.System)
    ]);

    public bool TryResolve(Type serviceType, out object? service)
    {
        return _composite.TryResolve(serviceType, out service);
    }
}

/// <summary>
/// 関数ベースの依存解決実装
/// </summary>
public sealed class DelegateCommandDependencyResolver(Func<Type, object?> resolver) : ICommandDependencyResolver
{
    private readonly Func<Type, object?> _resolver = EnsureNotNull(resolver);

    public bool TryResolve(Type serviceType, out object? service)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        service = _resolver(serviceType);
        return service is not null;
    }

    private static Func<Type, object?> EnsureNotNull(Func<Type, object?> value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value;
    }
}

/// <summary>
/// 型情報や依存関係を解決し、実行に必要なインスタンスを返します。
/// </summary>
internal sealed class CompositeCommandDependencyResolver(IReadOnlyList<ICommandDependencyResolver> resolvers)
    : ICommandDependencyResolver
{
    private readonly IReadOnlyList<ICommandDependencyResolver> _resolvers = EnsureNotNull(resolvers);

    public bool TryResolve(Type serviceType, out object? service)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        foreach (var resolver in _resolvers)
        {
            if (resolver.TryResolve(serviceType, out service))
            {
                return true;
            }
        }

        service = null;
        return false;
    }

    private static IReadOnlyList<ICommandDependencyResolver> EnsureNotNull(IReadOnlyList<ICommandDependencyResolver> value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value;
    }
}

/// <summary>
/// 型情報や依存関係を解決し、実行に必要なインスタンスを返します。
/// </summary>
internal sealed class CoreCommandDependencyResolver(
    IVariableStore variableStore,
    IObjectDetector objectDetector,
    IPathResolver pathResolver,
    IImageMatcher imageMatcher,
    IMouseInput mouseInput,
    IKeyboardInput keyboardInput,
    IScreenCapturer screenCapturer,
    IProcessLauncher processLauncher,
    IWindowService windowService,
    IOcrEngine ocrEngine) : ICommandDependencyResolver
{
    private readonly Dictionary<Type, object> _exactTypeMap = new()
    {
        [typeof(IVariableStore)] = variableStore,
        [typeof(IObjectDetector)] = objectDetector,
        [typeof(IPathResolver)] = pathResolver,
        [typeof(IImageMatcher)] = imageMatcher,
        [typeof(IMouseInput)] = mouseInput,
        [typeof(IKeyboardInput)] = keyboardInput,
        [typeof(IScreenCapturer)] = screenCapturer,
        [typeof(IProcessLauncher)] = processLauncher,
        [typeof(IWindowService)] = windowService,
        [typeof(IOcrEngine)] = ocrEngine
    };

    private readonly object[] _allDependencies =
    [
        variableStore,
        objectDetector,
        pathResolver,
        imageMatcher,
        mouseInput,
        keyboardInput,
        screenCapturer,
        processLauncher,
        windowService,
        ocrEngine
    ];

    public bool TryResolve(Type serviceType, out object? service)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (_exactTypeMap.TryGetValue(serviceType, out service))
        {
            return true;
        }

        foreach (var dependency in _allDependencies)
        {
            if (serviceType.IsInstanceOfType(dependency))
            {
                service = dependency;
                return true;
            }
        }

        service = null;
        return false;
    }
}

/// <summary>
/// 型情報や依存関係を解決し、実行に必要なインスタンスを返します。
/// </summary>
internal sealed class AmbientCommandDependencyResolver(
    ICommandEventBus? commandEventBus,
    TimeProvider timeProvider) : ICommandDependencyResolver
{
    private readonly Dictionary<Type, object> _exactTypeMap = BuildExactMap(commandEventBus, timeProvider);
    private readonly object[] _allDependencies = BuildAllDependencies(commandEventBus, timeProvider);

    public bool TryResolve(Type serviceType, out object? service)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (_exactTypeMap.TryGetValue(serviceType, out service))
        {
            return true;
        }

        foreach (var dependency in _allDependencies)
        {
            if (serviceType.IsInstanceOfType(dependency))
            {
                service = dependency;
                return true;
            }
        }

        service = null;
        return false;
    }

    private static Dictionary<Type, object> BuildExactMap(ICommandEventBus? commandEventBus, TimeProvider timeProvider)
    {
        Dictionary<Type, object> map = new()
        {
            [typeof(TimeProvider)] = timeProvider
        };

        if (commandEventBus is not null)
        {
            map[typeof(ICommandEventBus)] = commandEventBus;
        }

        return map;
    }

    private static object[] BuildAllDependencies(ICommandEventBus? commandEventBus, TimeProvider timeProvider)
    {
        if (commandEventBus is null)
        {
            return [timeProvider];
        }

        return [commandEventBus, timeProvider];
    }
}
