using AutoTool.Commands.DependencyInjection;
using AutoTool.Commands.Services;
using AutoTool.Panels.Model.CommandDefinition;
using AutoTool.Panels.Model.MacroFactory;
using AutoTool.Panels.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AutoTool.Panels.Hosting;

/// <summary>
/// Panelsのホスト構成を提供します
/// </summary>
public static class PanelsHostBuilder
{
    /// <summary>
    /// Panels用のホストビルダーを作成します
    /// </summary>
    /// <param name="args">コマンドライン引数</param>
    /// <returns>構成済みのホストビルダー</returns>
    public static IHostBuilder CreateHostBuilder(string[]? args = null)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // コマンドサービスを登録
                services.AddCommandServices();

                // コマンドファクトリを登録
                services.AddSingleton<ICommandFactory, CommandFactory>();

                // マクロファクトリを登録
                services.AddSingleton<IMacroFactory, MacroFactory>();

                // コマンドレジストリを登録
                services.AddSingleton<ICommandRegistry, CommandRegistryAdapter>();

                // ViewModelsを登録
                services.AddPanelsViewModels();
            });
    }

    /// <summary>
    /// Panels用のホストを構築して初期化します
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

    /// <summary>
    /// Panels のViewModelをDIコンテナに登録します
    /// </summary>
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


