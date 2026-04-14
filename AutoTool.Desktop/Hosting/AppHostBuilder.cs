using AutoTool.Infrastructure.Implementations;
using AutoTool.ViewModel;
using AutoTool.Commands.Services;
using AutoTool.Panels.Hosting;
using AutoTool.Panels.Model.CommandDefinition;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AutoTool.Core.Ports;

namespace AutoTool.Hosting;

/// <summary>
/// AutoTool�p�̃z�X�g�\����񋟂��܂�
/// </summary>
public static class AppHostBuilder
{
    /// <summary>
    /// AutoTool�p�̃z�X�g�r���_�[��쐬���܂�
    /// </summary>
    /// <param name="args">�R�}���h���C������</param>
    /// <returns>�\���ς݂̃z�X�g�r���_�[</returns>
    public static IHostBuilder CreateHostBuilder(string[]? args = null)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddPanelsCoreServices();

                // AutoTool �ŗL�̃T�[�r�X��o�^
                services.AddAutoToolServices();

                // Panels ��ViewModels��o�^
                services.AddPanelsViewModels();
            });
    }

    /// <summary>
    /// AutoTool�p�̃z�X�g��\�z���ď��������܂�
    /// </summary>
    /// <param name="args">�R�}���h���C������</param>
    /// <returns>�������ς݂̃z�X�g</returns>
    public static IHost BuildAndInitialize(string[]? args = null)
    {
        var host = CreateHostBuilder(args).Build();

        // �R�}���h���W�X�g���������
        host.Services.GetRequiredService<ICommandRegistry>().Initialize();

        return host;
    }
}

/// <summary>
/// AutoTool�ŗL�̃T�[�r�X�o�^�g��
/// </summary>
public static class AutoToolServiceExtensions
{
    /// <summary>
    /// AutoTool�ŗL�̃T�[�r�X��DI�R���e�i�ɓo�^���܂�
    /// </summary>
    public static IServiceCollection AddAutoToolServices(this IServiceCollection services)
    {
        // �ʒm�iCommands�̃C���^�[�t�F�[�X��g�p�j
        services.AddSingleton<AutoTool.Commands.Services.INotifier, WpfNotifier>();

        // �X�e�[�^�X���b�Z�[�W�X�P�W���[��
        services.AddSingleton<IStatusMessageScheduler, DispatcherStatusMessageScheduler>();

        // �t�@�C���_�C�A���O
        services.AddSingleton<IFilePicker, WpfFilePicker>();

        // �ŋߎg�����t�@�C���X�g�A
        services.AddSingleton<IRecentFileStore, XmlRecentFileStore>();
        services.AddSingleton<IFavoriteMacroStore, XmlFavoriteMacroStore>();

        // ���O
        services.AddSingleton<AutoTool.Infrastructure.AsyncFileLog>();
        services.AddSingleton<ILogWriter, DelegatingLogWriter>();

        // ViewModels
        services.AddTransient<MacroPanelViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MainWindow>();

        return services;
    }
}



