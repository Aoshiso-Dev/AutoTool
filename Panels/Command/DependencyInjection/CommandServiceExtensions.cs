using MacroPanels.Command.Infrastructure;
using MacroPanels.Command.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MacroPanels.Command.DependencyInjection;

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
        services.AddSingleton<IVariableStore, InMemoryVariableStore>();
        services.AddSingleton<IAIDetectionService, YoloDetectionService>();
        services.AddSingleton<IPathService, PathService>();

        // トランジェントサービス（ステートレスなもの）
        services.AddTransient<IImageSearchService, OpenCVImageSearchService>();
        services.AddTransient<IMouseService, Win32MouseService>();
        services.AddTransient<IKeyboardService, Win32KeyboardService>();
        services.AddTransient<IScreenCaptureService, OpenCVScreenCaptureService>();
        services.AddTransient<IProcessService, ProcessService>();
        services.AddTransient<IWindowService, Win32WindowService>();

        return services;
    }
}
