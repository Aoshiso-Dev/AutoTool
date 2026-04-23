using AutoTool.Application.Ports;
using AutoTool.Desktop.CommandLine;
using AutoTool.Desktop.Panels.Hosting;
using AutoTool.Desktop.Services;
using AutoTool.Desktop.Ui;
using AutoTool.Desktop.ViewModel;
using AutoTool.Infrastructure.Implementations;
using AutoTool.Plugin.Host.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace AutoTool.Desktop.Hosting;

/// <summary>
/// アプリ起動時の Host 構成を組み立て、`Settings/appsettings.json` の読み込みと各サービスの DI 登録を行います。
/// </summary>
public static class AppHostBuilder
{
    public static IHostBuilder CreateHostBuilder(CommandLineInvocation? startupInvocation = null, string[]? args = null)
    {
        var invocation = startupInvocation ?? CommandLineInvocation.Empty;

        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((_, configuration) =>
            {
                // インストール単位で設定を分離できるよう、Settings 配下の設定ファイルを優先します。
                configuration.AddJsonFile("Settings/appsettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton(invocation);
                services.AddPanelsCoreServices(context.Configuration);
                services.AddPluginHostServices(options =>
                {
                    options.RootDirectoryPath = Path.Combine(AppContext.BaseDirectory, "Plugins");
                });
                services.AddAutoToolServices();
                services.AddPanelsViewModels();
            });
    }

}

/// <summary>
/// AutoTool 本体で利用する通知・ダイアログ・保存ストア・ViewModel などを DI コンテナへ登録する拡張メソッドを提供します。
/// </summary>
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
        services.AddSingleton<IRecentFileStore, JsonRecentFileStore>();
        services.AddSingleton<IFavoriteMacroStore, JsonFavoriteMacroStore>();
        services.AddSingleton<IUiStatePreferenceStore, JsonUiStatePreferenceStore>();

        services.AddSingleton<AutoTool.Infrastructure.AsyncFileLog>();
        services.AddSingleton<ILogWriter, DelegatingLogWriter>();
        services.AddSingleton<PluginStartupDiagnosticsPresenter>();

        services.AddSingleton<MacroPanelViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<CommandLineControlService>();
        services.AddHostedService<MainWindowHostedService>();
        services.AddHostedService<CommandLineIpcHostedService>();

        return services;
    }
}





