using AutoTool.Commands.Infrastructure;
using AutoTool.Commands.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AutoTool.Commands.DependencyInjection;

/// <summary>
/// コマンドサービスのDI拡張メソッド
/// </summary>
public static class CommandServiceExtensions
{
    /// <summary>
    /// コマンド実行に必要なサービスをDIコンテナに登録します
    /// </summary>
    /// <param name="services">サービスコレクション</param>
    /// <returns>サービスコレクション</returns>
    public static IServiceCollection AddCommandServices(this IServiceCollection services)
    {
        // シングルトンサービス（状態を持つ、または高コストな初期化を持つ）
        services.AddSingleton<ICommandEventBus, CommandEventBus>();
        services.AddSingleton<IVariableStore, InMemoryVariableStore>();
        services.AddSingleton<IObjectDetector, YoloObjectDetector>();
        services.AddSingleton<IPathResolver, PathResolver>();

        // トランジェントサービス（ステートレスなもの）
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

