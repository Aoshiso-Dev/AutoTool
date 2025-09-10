using Microsoft.Extensions.DependencyInjection;


namespace AutoTool.Core.Descriptors;


public static class ServiceCollectionExtensions
{
    /// <summary>
    /// AutoToolのDescriptor登録補助。各アセンブリで AddSingleton<ICommandDescriptor, T>() を並べ、最後にこれを呼ぶ。
    /// </summary>
    public static IServiceCollection AddAutoToolCommandRegistry(this IServiceCollection services)
    {
        services.AddSingleton<ICommandRegistry, CommandRegistry>();
        return services;
    }
}