// AutoTool.Commands.* の Descriptor を登録
using AutoTool.Commands.Flow.If;
using AutoTool.Commands.Flow.While;
using AutoTool.Core.Descriptors;
using AutoTool.Core.Runtime;
using Microsoft.Extensions.DependencyInjection;


namespace AutoTool.Desktop.Startup;


public static class ServiceRegistration
{
    public static IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();


        // Core
        services.AddAutoToolCommandRegistry();


        // Descriptors（1型=1行でAddSingleton）
        services.AddSingleton<ICommandDescriptor, IfDescriptor>();
        services.AddSingleton<ICommandDescriptor, WhileDescriptor>();

        services.AddSingleton<ICommandRegistry, CommandRegistry>(sp =>
        {
            var descriptors = sp.GetServices<ICommandDescriptor>();
            return new CommandRegistry(descriptors);
        });

        services.AddSingleton<ICommandRunner, CommandRunner>();


        // ここに他の実行時サービス（IExecutionContextの実装、IHttpClientFactoryなど）も登録
        // services.AddSingleton<IExecutionContext, ExecutionContext>();


        var provider = services.BuildServiceProvider();
        CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.ConfigureServices(provider);
        return provider;
    }
}