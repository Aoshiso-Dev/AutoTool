using AutoTool.Core.Ports;
using AutoTool.Infrastructure.Implementations;
using AutoTool.Panels.Hosting;
using AutoTool.Panels.Model.CommandDefinition;
using AutoTool.ViewModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AutoTool.Hosting;

public static class AppHostBuilder
{
    public static IHostBuilder CreateHostBuilder(string[]? args = null)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((_, configuration) =>
            {
                // Prefer per-install settings under the Settings folder.
                configuration.AddJsonFile("Settings/appsettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddPanelsCoreServices(context.Configuration);
                services.AddAutoToolServices();
                services.AddPanelsViewModels();
            });
    }

    public static IHost BuildAndInitialize(string[]? args = null)
    {
        var host = CreateHostBuilder(args).Build();
        host.Services.GetRequiredService<ICommandRegistry>().Initialize();
        return host;
    }
}

public static class AutoToolServiceExtensions
{
    public static IServiceCollection AddAutoToolServices(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<AutoTool.Commands.Services.INotifier, WpfNotifier>();
        services.AddSingleton<IStatusMessageScheduler, DispatcherStatusMessageScheduler>();
        services.AddSingleton<IFilePicker, WpfFilePicker>();
        services.AddSingleton<IRecentFileStore, XmlRecentFileStore>();
        services.AddSingleton<IFavoriteMacroStore, XmlFavoriteMacroStore>();

        services.AddSingleton<AutoTool.Infrastructure.AsyncFileLog>();
        services.AddSingleton<ILogWriter, DelegatingLogWriter>();

        services.AddTransient<MacroPanelViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MainWindow>();

        return services;
    }
}
