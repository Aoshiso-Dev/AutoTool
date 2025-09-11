// AutoTool.Commands.* の Descriptor を登録
using AutoTool.Commands.Flow.If;
using AutoTool.Commands.Flow.While;
using AutoTool.Commands.Flow.Wait;
using AutoTool.Commands.Input.Click;
using AutoTool.Commands.Input.KeyInput;
using AutoTool.Core.Abstractions;
using AutoTool.Core.Descriptors;
using AutoTool.Core.Runtime;
using AutoTool.Core.Registration;
using AutoTool.Core.Services;
using AutoTool.Services;
using AutoTool.Desktop.ViewModels;
using AutoTool.Desktop.Views;
using AutoTool.Desktop.Runtime;
using AutoTool.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace AutoTool.Desktop.Startup;

public static class ServiceRegistration
{
    /// <summary>
    /// IHostを構築して返す
    /// </summary>
    public static IHost BuildHost()
    {
        var exeDir = AppContext.BaseDirectory;

        return Host.CreateDefaultBuilder()
            .UseContentRoot(exeDir)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(exeDir);
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                
                // 開発環境用の設定ファイルも追加
                var env = context.HostingEnvironment.EnvironmentName;
                config.AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                
                // 設定ファイルからログレベルを読み込み
                logging.AddConfiguration(context.Configuration.GetSection("Logging"));
            })
            .ConfigureServices((context, services) =>
            {
                // AutoToolの全サービスを登録
                RegisterAutoToolServices(services);
            })
            .UseConsoleLifetime(options => options.SuppressStatusMessages = false)
            .Build();
    }

    /// <summary>
    /// AutoToolのサービスをすべて登録
    /// </summary>
    private static void RegisterAutoToolServices(IServiceCollection services)
    {
        try
        {
            // Core Services - 基本的な実装のみ
            services.AddLogging();

            // Traditional Descriptors (確実に動作するもののみ)
            services.AddSingleton<ICommandDescriptor, IfDescriptor>();
            services.AddSingleton<ICommandDescriptor, WhileDescriptor>();
            services.AddSingleton<ICommandDescriptor, WaitDescriptor>();
            services.AddSingleton<ICommandDescriptor, ClickDescriptor>();
            services.AddSingleton<ICommandDescriptor, KeyInputDescriptor>();

            // Basic Command Registry 
            services.AddSingleton<ICommandRegistry>(serviceProvider =>
            {
                var descriptors = serviceProvider.GetServices<ICommandDescriptor>();
                return new CommandRegistry(descriptors);
            });

            services.AddSingleton<ICommandRunner, CommandRunner>();

            // Runtime実装
            services.AddSingleton<IValueResolver, SimpleValueResolver>();
            services.AddSingleton<IVariableScope, SimpleVariableScope>();
            services.AddSingleton<IExecutionContext>(sp =>
            {
                var valueResolver = sp.GetRequiredService<IValueResolver>();
                var variables = sp.GetRequiredService<IVariableScope>();
                var logger = sp.GetRequiredService<ILogger<AutoTool.Desktop.Runtime.ExecutionContext>>();
                return new AutoTool.Desktop.Runtime.ExecutionContext(valueResolver, variables, logger);
            });

            // ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<ButtonPanelViewModel>();
            services.AddTransient<EditPanelViewModel>();
            
            // Views
            services.AddTransient<MainWindow>(serviceProvider =>
            {
                var mainViewModel = serviceProvider.GetRequiredService<MainViewModel>();
                var buttonPanelViewModel = serviceProvider.GetRequiredService<ButtonPanelViewModel>();
                var editPanelViewModel = serviceProvider.GetRequiredService<EditPanelViewModel>();
                
                return new MainWindow(mainViewModel, buttonPanelViewModel, editPanelViewModel);
            });

            // WPF Application Service
            services.AddHostedService<WpfApplicationService>();

            // Attribute-based Command Registration (オプション)
            TryAddAttributeBasedCommandRegistration(services);
        }
        catch (Exception ex)
        {
            // サービス登録でエラーが発生した場合でも、基本的なサービスは登録する
            Console.WriteLine($"Service registration error: {ex.Message}");
            
            // 最小限のサービス構成
            services.AddLogging();
            services.AddTransient<MainViewModel>();
            services.AddTransient<ButtonPanelViewModel>();
            services.AddTransient<EditPanelViewModel>();
            services.AddTransient<MainWindow>();
            services.AddHostedService<WpfApplicationService>();
        }
    }

    /// <summary>
    /// Attribute-based Command Registrationを安全に追加
    /// </summary>
    private static void TryAddAttributeBasedCommandRegistration(IServiceCollection services)
    {
        try
        {
            services.AddSingleton<AttributeCommandRegistrationService>();
            
            // DynamicCommandRegistryを試行
            services.AddSingleton<IDynamicCommandRegistry>(serviceProvider =>
            {
                var logger = serviceProvider.GetService<ILogger<DynamicCommandRegistry>>();
                var registry = new DynamicCommandRegistry(null, logger);
                
                // 自動登録を試行
                try
                {
                    var registrationService = serviceProvider.GetService<AttributeCommandRegistrationService>();
                    if (registrationService != null)
                    {
                        var count = registrationService.RegisterCommandsFromCurrentDomain(registry);
                        logger?.LogInformation("Attribute-based commands registered: {Count}", count);
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Failed to register attribute-based commands");
                }
                
                return registry;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Attribute-based command registration failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 従来のServiceProvider方式との後方互換性のためのメソッド
    /// </summary>
    [Obsolete("Use BuildHost() instead. This method is provided for backward compatibility.")]
    public static IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        // 基本的なサービスのみ登録
        services.AddLogging(builder => builder.AddConsole().AddDebug());

        // Traditional Descriptors
        services.AddSingleton<ICommandDescriptor, WaitDescriptor>();
        services.AddSingleton<ICommandDescriptor, ClickDescriptor>();

        services.AddSingleton<ICommandRegistry>(sp =>
        {
            var descriptors = sp.GetServices<ICommandDescriptor>();
            return new CommandRegistry(descriptors);
        });

        services.AddSingleton<ICommandRunner, CommandRunner>();
        services.AddSingleton<IValueResolver, SimpleValueResolver>();
        services.AddSingleton<IVariableScope, SimpleVariableScope>();

        services.AddTransient<MainViewModel>();
        services.AddTransient<ButtonPanelViewModel>();
        services.AddTransient<EditPanelViewModel>();

        return services.BuildServiceProvider();
    }
}