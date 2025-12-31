using AutoTool.Services.Implementations;
using AutoTool.ViewModel;
using MacroPanels.Command.DependencyInjection;
using MacroPanels.Command.Services;
using MacroPanels.Hosting;
using MacroPanels.Model.CommandDefinition;
using MacroPanels.Model.MacroFactory;
using MacroPanels.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AutoTool.Services.Interfaces;

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
                // MacroPanels のコマンドサービスを登録
                services.AddCommandServices();

                // コマンドファクトリを登録
                services.AddSingleton<ICommandFactory, CommandFactory>();

                // マクロファクトリを登録
                services.AddSingleton<IMacroFactory, MacroFactory>();

                // コマンドレジストリを登録
                services.AddSingleton<ICommandRegistry, CommandRegistryAdapter>();

                // AutoTool 固有のサービスを登録
                services.AddAutoToolServices();

                // MacroPanels のViewModelsを登録
                services.AddMacroPanelViewModels();
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
        // 通知サービス（MacroPanelsのインターフェースを使用）
        services.AddSingleton<MacroPanels.Command.Services.INotificationService, WpfNotificationService>();

        // ステータスメッセージスケジューラ
        services.AddSingleton<IStatusMessageScheduler, DispatcherStatusMessageScheduler>();

        // ファイルダイアログサービス
        services.AddSingleton<IFileDialogService, WpfFileDialogService>();

        // 最近使ったファイルストア
        services.AddSingleton<IRecentFileStore, XmlRecentFileStore>();

        // ログサービス
        services.AddSingleton<ILogService, LogHelperLogger>();

        // ViewModels
        services.AddTransient<MacroPanelViewModel>();
        services.AddTransient<MainWindowViewModel>();

        return services;
    }
}
