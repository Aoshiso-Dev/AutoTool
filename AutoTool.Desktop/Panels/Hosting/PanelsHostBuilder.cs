using AutoTool.Commands.DependencyInjection;
using AutoTool.Commands.Services;
using AutoTool.Core.Ports;
using AutoTool.Infrastructure.Implementations;
using AutoTool.Panels.Model.CommandDefinition;
using AutoTool.Panels.Model.MacroFactory;
using AutoTool.Panels.List.Class;
using AutoTool.Panels.Serialization;
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
                services.AddTransient<ICompositeCommandBuilder, IfCompositeCommandBuilder>();
                services.AddTransient<ICompositeCommandBuilder, LoopCompositeCommandBuilder>();
                services.AddSingleton<IMacroFactory, MacroFactory>();

                // コマンドレジストリ/定義プロバイダーを登録
                services.AddSingleton<ReflectionCommandRegistry>();
                services.AddSingleton<ICommandRegistry>(sp => sp.GetRequiredService<ReflectionCommandRegistry>());
                services.AddSingleton<ICommandDefinitionProvider>(sp => sp.GetRequiredService<ReflectionCommandRegistry>());
                services.AddTransient<IMacroFileSerializer, MacroFileSerializer>();
                services.AddTransient<CommandList>();

                // Panels向けUIサービス
                services.AddSingleton<IPanelDialogService, WpfPanelDialogService>();
                services.AddSingleton<ICapturePathProvider, CapturePathProvider>();

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


