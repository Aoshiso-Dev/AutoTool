using AutoTool.Services.Abstractions;
using AutoTool.Services.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoTool.Services;

/// <summary>
/// AutoTool.Services プロジェクトのサービス登録
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// AutoTool.Services の全サービスを DI コンテナに登録
    /// </summary>
    public static IServiceCollection AddAutoToolServices(this IServiceCollection services)
    {
        // ログ関連
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // UI関連サービス
        services.AddSingleton<IPluginService, PluginService>();

        // ファイル・設定関連
        services.AddSingleton<RecentFileService>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        // キャプチャ・入出力関連
        services.AddSingleton<ICaptureService, CaptureService>();
        services.AddSingleton<IColorPickService, ColorPickService>();
        services.AddSingleton<IAdvancedColorPickingService, AdvancedColorPickingService>();
        services.AddSingleton<IWindowCaptureService, WindowCaptureService>();
        services.AddSingleton<IKeyCaptureService, KeyCaptureService>();
        services.AddSingleton<IMouseService, MouseService>();
        services.AddSingleton<IKeyboardService, KeyboardService>();
        services.AddSingleton<IUIService, UIService>();

        // 物体検出関連（YOLO）
        services.AddSingleton<IObjectDetectionService, ObjectDetectionService>();

        return services;
    }
}

