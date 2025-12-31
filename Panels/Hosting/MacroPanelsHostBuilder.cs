using MacroPanels.Command.DependencyInjection;
using MacroPanels.Command.Services;
using MacroPanels.Model.CommandDefinition;
using MacroPanels.Model.MacroFactory;
using MacroPanels.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MacroPanels.Hosting;

/// <summary>
/// MacroPanelsのホスト構成を提供します
/// </summary>
public static class MacroPanelsHostBuilder
{
    /// <summary>
    /// MacroPanels用のホストビルダーを作成します
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
                services.AddMacroPanelViewModels();
            });
    }

    /// <summary>
    /// MacroPanels用のホストを構築して初期化します
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
    /// MacroPanels のViewModelをDIコンテナに登録します
    /// </summary>
    public static IServiceCollection AddMacroPanelViewModels(this IServiceCollection services)
    {
        services.AddTransient<IListPanelViewModel, ListPanelViewModel>();
        services.AddTransient<IEditPanelViewModel, EditPanelViewModel>();
        services.AddTransient<IButtonPanelViewModel, ButtonPanelViewModel>();
        services.AddTransient<ILogPanelViewModel, LogPanelViewModel>();
        services.AddTransient<IFavoritePanelViewModel, FavoritePanelViewModel>();

        return services;
    }
}
