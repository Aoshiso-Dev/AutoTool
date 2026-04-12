using AutoTool.Infrastructure.Implementations;
using AutoTool.ViewModel;
using AutoTool.Commands.DependencyInjection;
using AutoTool.Commands.Services;
using AutoTool.Panels.Hosting;
using AutoTool.Panels.List.Class;
using AutoTool.Panels.Model.CommandDefinition;
using AutoTool.Panels.Model.MacroFactory;
using AutoTool.Panels.Serialization;
using AutoTool.Panels.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AutoTool.Core.Ports;

namespace AutoTool.Hosting;

/// <summary>
/// AutoTool用のホスト構成を提供します
/// </summary>
public static class AppHostBuilder
{
    /// <summary>
    /// AutoTool用のホストビルダーを作成します
    /// </summary>
    /// <param name="args">コマンドライン引数</param>
    /// <returns>構成済みのホストビルダー</returns>
    public static IHostBuilder CreateHostBuilder(string[]? args = null)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Panels のコマンドサービスを登録
                services.AddCommandServices();

                // コマンドファクトリを登録
                services.AddSingleton<ICommandFactory, CommandFactory>();

                // マクロファクトリを登録
                services.AddTransient<ICompositeCommandBuilder, IfCompositeCommandBuilder>();
                services.AddTransient<ICompositeCommandBuilder, LoopCompositeCommandBuilder>();
                services.AddSingleton<IMacroFactory, MacroFactory>();

                // コマンドレジストリ/定義プロバイダーを登録
                services.AddSingleton<ReflectionCommandRegistry>();
                services.AddSingleton<ICommandRegistry>(sp => sp.GetRequiredService<ReflectionCommandRegistry>());
                services.AddSingleton<ICommandDefinitionProvider>(sp => sp.GetRequiredService<ReflectionCommandRegistry>());
                services.AddTransient<IMacroFileSerializer, MacroFileSerializer>();
                services.AddTransient<CommandList>();

                // AutoTool 固有のサービスを登録
                services.AddAutoToolServices();

                // Panels のViewModelsを登録
                services.AddPanelsViewModels();
            });
    }

    /// <summary>
    /// AutoTool用のホストを構築して初期化します
    /// </summary>
    /// <param name="args">コマンドライン引数</param>
    /// <returns>初期化済みのホスト</returns>
    public static IHost BuildAndInitialize(string[]? args = null)
    {
        var host = CreateHostBuilder(args).Build();

        // コマンドレジストリを初期化
        host.Services.GetRequiredService<ICommandRegistry>().Initialize();

        return host;
    }
}

/// <summary>
/// AutoTool固有のサービス登録拡張
/// </summary>
public static class AutoToolServiceExtensions
{
    /// <summary>
    /// AutoTool固有のサービスをDIコンテナに登録します
    /// </summary>
    public static IServiceCollection AddAutoToolServices(this IServiceCollection services)
    {
        // 通知（Commandsのインターフェースを使用）
        services.AddSingleton<AutoTool.Commands.Services.INotifier, WpfNotifier>();

        // ステータスメッセージスケジューラ
        services.AddSingleton<IStatusMessageScheduler, DispatcherStatusMessageScheduler>();

        // ファイルダイアログ
        services.AddSingleton<IFilePicker, WpfFilePicker>();

        // 最近使ったファイルストア
        services.AddSingleton<IRecentFileStore, XmlRecentFileStore>();

        // ログ
        services.AddSingleton<AutoTool.Infrastructure.AsyncFileLog>();
        services.AddSingleton<ILogWriter, DelegatingLogWriter>();

        // Panels 向けUIサービス
        services.AddSingleton<IPanelDialogService, WpfPanelDialogService>();
        services.AddSingleton<ICapturePathProvider, CapturePathProvider>();

        // ViewModels
        services.AddTransient<MacroPanelViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MainWindow>();

        return services;
    }
}



