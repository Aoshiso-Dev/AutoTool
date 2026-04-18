using AutoTool.Commands.Infrastructure;
using AutoTool.Commands.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AutoTool.Commands.DependencyInjection;

public static class CommandServiceExtensions
{
    public static IServiceCollection AddCommandServices(
        this IServiceCollection services,
        Action<CommandEventBusOptions>? configureCommandEventBus = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        CommandEventBusOptions commandEventBusOptions = new();
        configureCommandEventBus?.Invoke(commandEventBusOptions);

        services.AddSingleton(commandEventBusOptions);

        services.AddSingleton<ICommandEventBus, CommandEventBus>();
        services.AddSingleton<IVariableStore, InMemoryVariableStore>();
        services.AddSingleton<IObjectDetector, YoloObjectDetector>();
        services.AddSingleton<IPathResolver, PathResolver>();

        services.AddTransient<IImageMatcher, OpenCvImageMatcher>();
        services.AddTransient<IMouseInput, Win32MouseInput>();
        services.AddTransient<IKeyboardInput, Win32KeyboardInput>();
        services.AddTransient<IScreenCapturer, OpenCvScreenCapturer>();
        services.AddTransient<IProcessLauncher, ProcessLauncher>();
        services.AddTransient<IWindowService, Win32WindowService>();
        services.AddTransient<IOcrEngine, TesseractOcrEngine>();

        return services;
    }
}
