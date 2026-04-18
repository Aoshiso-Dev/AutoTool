using AutoTool.Commands.DependencyInjection;
using AutoTool.Application.Ports;
using AutoTool.Automation.Runtime.Definitions;
using AutoTool.Automation.Runtime.Lists;
using AutoTool.Automation.Runtime.MacroFactory;
using AutoTool.Automation.Runtime.Serialization;
using Microsoft.Extensions.DependencyInjection;
using RuntimeMacroFactory = AutoTool.Automation.Runtime.MacroFactory.MacroFactory;

namespace AutoTool.Infrastructure.DependencyInjection;

public static class MacroRuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddMacroRuntimeCoreServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ICommandDependencyResolver, CommandDependencyResolver>();
        services.AddSingleton<ICommandFactory, CommandFactory>();

        services.AddTransient<ICompositeCommandBuilder, IfCompositeCommandBuilder>();
        services.AddTransient<ICompositeCommandBuilder, LoopCompositeCommandBuilder>();
        services.AddSingleton<IMacroFactory, RuntimeMacroFactory>();

        services.AddSingleton<ReflectionCommandRegistry>();
        services.AddSingleton<ICommandRegistry, ReflectionCommandRegistry>();
        services.AddSingleton<ICommandDefinitionProvider, ReflectionCommandRegistry>();
        services.AddTransient<IMacroFileSerializer, MacroFileSerializer>();
        services.AddTransient<ICommandListFileGateway, CommandListFileGateway>();
        services.AddTransient<CommandList>();

        return services;
    }
}
