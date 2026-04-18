using AutoTool.Commands.DependencyInjection;
using AutoTool.Commands.Infrastructure;
using AutoTool.Commands.Services;
using AutoTool.Core.Ports;
using AutoTool.Infrastructure.Implementations;
using AutoTool.Panels.List.Class;
using AutoTool.Panels.Model.CommandDefinition;
using AutoTool.Panels.Model.MacroFactory;
using AutoTool.Panels.Serialization;
using AutoTool.Panels.ViewModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AutoTool.Panels.Hosting;

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

        services.AddSingleton<ICommandDependencyResolver>(sp => new DelegateCommandDependencyResolver(sp.GetService));
        services.AddSingleton<ICommandFactory, CommandFactory>();

        services.AddTransient<ICompositeCommandBuilder, IfCompositeCommandBuilder>();
        services.AddTransient<ICompositeCommandBuilder, LoopCompositeCommandBuilder>();
        services.AddSingleton<IMacroFactory, MacroFactory>();

        services.AddSingleton<ReflectionCommandRegistry>();
        services.AddSingleton<ICommandRegistry>(sp => sp.GetRequiredService<ReflectionCommandRegistry>());
        services.AddSingleton<ICommandDefinitionProvider>(sp => sp.GetRequiredService<ReflectionCommandRegistry>());
        services.AddHostedService<CommandRegistryInitializationHostedService>();
        services.AddTransient<IMacroFileSerializer, MacroFileSerializer>();
        services.AddTransient<CommandList>();

        services.AddSingleton<IPanelDialogService, WpfPanelDialogService>();
        services.AddSingleton<ICapturePathProvider, CapturePathProvider>();
        services.AddSingleton<IFavoriteMacroStore, XmlFavoriteMacroStore>();

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
