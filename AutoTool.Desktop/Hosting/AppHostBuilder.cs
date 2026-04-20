using AutoTool.Application.Ports;
using AutoTool.Desktop.Panels.Hosting;
using AutoTool.Desktop.Services;
using AutoTool.Desktop.Ui;
using AutoTool.Desktop.ViewModel;
using AutoTool.Infrastructure.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AutoTool.Desktop.Hosting;

public static class AppHostBuilder
{
    public static IHostBuilder CreateHostBuilder(string[]? args = null)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((_, configuration) =>
            {
                // インストール単位で設定を分離できるよう、Settings 配下の設定ファイルを優先します。
                configuration.AddJsonFile("Settings/appsettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddPanelsCoreServices(context.Configuration);
                services.AddAutoToolServices();
                services.AddPanelsViewModels();
            });
    }

}

public static class AutoToolServiceExtensions
{
    public static IServiceCollection AddAutoToolServices(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<AutoTool.Commands.Services.INotifier, WpfNotifier>();
        services.AddSingleton<IAppDialogService, WpfAppDialogService>();
        services.AddSingleton<IStatusMessageScheduler, DispatcherStatusMessageScheduler>();
        services.AddSingleton<IFilePicker, WpfFilePicker>();
        services.AddSingleton<IFileSystemPathService, FileSystemPathService>();
        services.AddSingleton<IRecentFileStore, XmlRecentFileStore>();
        services.AddSingleton<IFavoriteMacroStore, XmlFavoriteMacroStore>();
        services.AddSingleton<IUiStatePreferenceStore, JsonUiStatePreferenceStore>();

        services.AddSingleton<AutoTool.Infrastructure.AsyncFileLog>();
        services.AddSingleton<ILogWriter, DelegatingLogWriter>();

        services.AddTransient<MacroPanelViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MainWindow>();
        services.AddHostedService<MainWindowHostedService>();

        return services;
    }
}
