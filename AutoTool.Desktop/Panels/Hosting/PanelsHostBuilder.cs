using AutoTool.Commands.DependencyInjection;
using AutoTool.Commands.Infrastructure;
using AutoTool.Application.Ports;
using AutoTool.Infrastructure.DependencyInjection;
using AutoTool.Infrastructure.Implementations;
using AutoTool.Desktop.Panels.Services;
using AutoTool.Desktop.Panels.ViewModel;
using AutoTool.Desktop.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AutoTool.Desktop.Panels.Hosting;

/// <summary>
/// パネル機能向けの Host 構成を組み立て、必要なサービスと ViewModel を登録します。
/// </summary>
public static class PanelsHostBuilder
{
    public static IHostBuilder CreateHostBuilder(string[]? args = null)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddPanelsCoreServices(context.Configuration);
                services.AddPanelsViewModels();
            });
    }

    public static IServiceCollection AddPanelsCoreServices(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddCommandServices(options =>
        {
            configuration?.GetSection(CommandEventBusOptions.SectionName).Bind(options);
        });

        services.AddMacroRuntimeCoreServices();
        services.AddHostedService<CommandRegistryInitializationHostedService>();

        services.AddSingleton<IPanelDialogService, WpfPanelDialogService>();
        services.AddSingleton<ICapturePathProvider, CapturePathProvider>();
        services.AddSingleton<IDetectionHighlightService, DetectionHighlightService>();
        services.AddSingleton<IFavoriteMacroStore, JsonFavoriteMacroStore>();

        return services;
    }

    public static IServiceCollection AddPanelsViewModels(this IServiceCollection services)
    {
        services.AddTransient<IListPanelViewModel, ListPanelViewModel>();
        services.AddTransient<IEditPanelViewModel, EditPanelViewModel>();
        services.AddTransient<IButtonPanelViewModel, ButtonPanelViewModel>();
        services.AddTransient<ILogPanelViewModel, LogPanelViewModel>();
        services.AddTransient<IFavoritePanelViewModel, FavoritePanelViewModel>();

        return services;
    }
}
