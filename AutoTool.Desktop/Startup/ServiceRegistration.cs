using AutoTool.Core.Abstractions;
using AutoTool.Core.Descriptors;
using AutoTool.Core.Registration;
using AutoTool.Core.Runtime;
using AutoTool.Core.Services;
using AutoTool.Desktop.Runtime;
using AutoTool.Desktop.Services;
using AutoTool.Desktop.ViewModels;
using AutoTool.Desktop.Views;
using AutoTool.Desktop.Views.Parts;
using AutoTool.Services;
using AutoTool.Services.Abstractions;
using AutoTool.Services.Implementations;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

            // Attribute-based Command Registration System (新システム)
            services.AddSingleton<AttributeCommandRegistrationService>();

            // Dynamic Command Registry (Attributeベースのみ) 
            services.AddSingleton<ICommandRegistry>(serviceProvider =>
            {
                var logger = serviceProvider.GetService<ILogger<DynamicCommandRegistry>>();
                var registry = new DynamicCommandRegistry(null, logger);

                // Attribute-based Commandsを自動登録
                try
                {
                    var registrationService = serviceProvider.GetRequiredService<AttributeCommandRegistrationService>();
                    var count = registrationService.RegisterCommandsFromCurrentDomain(registry);
                    logger?.LogInformation("✅ Attribute-based commands registered: {Count}", count);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "⚠️ Failed to register attribute-based commands");
                }

                return registry;
            });

            // ICommandRunner 登録（イベント → Messenger ブリッジ付き）
            services.AddSingleton<ICommandRunner>(sp =>
            {
                var logger = sp.GetService<ILogger<CommandRunner>>();
                var runner = new CommandRunner();

                var messenger = CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default;

                runner.CommandStarting += (_, cmd) =>
                {
                    if (!cmd.IsEnabled) return;
                    logger?.LogDebug("[Runner] CommandStarting: {Type}", cmd.Type);
                    messenger.Send(new CommandExecutionStartMessage(cmd));
                };

                runner.CommandFinished += (_, result) =>
                {
                    logger?.LogDebug("[Runner] CommandFinished: {Type} -> {Flow}", result.cmd.Type, result.result);
                    messenger.Send(new CommandExecutionEndMessage(result.cmd));
                };

                logger?.LogInformation("ICommandRunner initialized and bridged to Messenger");
                return runner;
            });
            
            // Services
            services.AddSingleton<IValueResolver, SimpleValueResolver>();
            services.AddSingleton<IVariableScope, SimpleVariableScope>();
            services.AddSingleton<ICaptureService, CaptureService>();
            services.AddSingleton<IMouseService, MouseService>();
            services.AddSingleton<IKeyboardService, KeyboardService>();
            services.AddSingleton<IWindowCaptureService, WindowCaptureService>();
            services.AddSingleton<IImageService, ImageService>();
            services.AddSingleton<IUIService, UIService>();

            // Views
            services.AddSingleton<MainWindow>();
            services.AddSingleton<ButtonPanel>();
            services.AddSingleton<EditPanel>();
            services.AddSingleton<ListPanel>();

            // ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<ButtonPanelViewModel>();
            services.AddTransient<EditPanelViewModel>();
            services.AddTransient<ListPanelViewModel>();

            // Views
            services.AddTransient<MainWindow>();

            // WPF Application Service
            services.AddHostedService<WpfApplicationService>();

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
}