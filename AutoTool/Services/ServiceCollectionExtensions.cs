using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Plugin;
using AutoTool.ViewModel;
using AutoTool.ViewModel.Panels;
using AutoTool.List.Class;
using AutoTool.Model.List.Type;
using AutoTool.Model.CommandDefinition;
using AutoTool.Model.List.Interface;
using AutoTool.Helpers;
using System.Text.Json;
using CommunityToolkit.Mvvm.Messaging;
using System;

namespace AutoTool.Services
{
    /// <summary>
    /// �T�[�r�X�R���N�V�����g�����\�b�h
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// AutoTool�̑S�T�[�r�X��o�^
        /// </summary>
        public static IServiceCollection AddAutoToolServices(this IServiceCollection services)
        {
            // ���M���O�ݒ�
            services.AddLogging(builder =>
            {
                builder.AddDebug();
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Messaging�ݒ�
            services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

            // �v���O�C���T�[�r�X
            services.AddSingleton<IPluginService, PluginService>();

            // �t�@�C���T�[�r�X
            services.AddSingleton<IRecentFileService, RecentFileService>();

            // �ϐ��X�g�A�T�[�r�X�iAutoTool�ł�o�^�j
            services.AddSingleton<AutoTool.Services.IVariableStore, AutoTool.Services.VariableStore>();

            // �}�N�����s�T�[�r�X
            services.AddSingleton<AutoTool.Services.Execution.IMacroExecutionService, AutoTool.Services.Execution.MacroExecutionService>();

            // �ݒ�Ǘ��T�[�r�X
            services.AddSingleton<AutoTool.Services.Configuration.IEnhancedConfigurationService, AutoTool.Services.Configuration.EnhancedConfigurationService>();

            // UI�֘A�T�[�r�X�iView-ViewModel DI�Ή��j
            services.AddSingleton<AutoTool.Services.UI.IDataContextLocator, AutoTool.Services.UI.DataContextLocator>();
            services.AddSingleton<AutoTool.Services.UI.IUIStateService, AutoTool.Services.UI.UIStateService>();
            services.AddSingleton<AutoTool.Services.UI.IWindowSettingsService, AutoTool.Services.UI.WindowSettingsService>();
            services.AddSingleton<AutoTool.Services.UI.IEnhancedThemeService, AutoTool.Services.UI.EnhancedThemeService>();
            services.AddSingleton<AutoTool.Services.UI.IAppSettingsService, AutoTool.Services.UI.AppSettingsService>();
            
            // EditPanelPropertyService (�p�~�\��) - ����݊����̂��߈ꎞ�I�ɕێ�
            services.AddSingleton<AutoTool.Services.UI.IEditPanelPropertyService, AutoTool.Services.UI.EditPanelPropertyService>();
            
            // MainWindow�֘A�T�[�r�X
            services.AddSingleton<AutoTool.Services.UI.IMainWindowMenuService, AutoTool.Services.UI.MainWindowMenuService>();
            services.AddSingleton<AutoTool.Services.UI.IMainWindowButtonService, AutoTool.Services.UI.MainWindowButtonService>();

            // �A�v���P�[�V�����������T�[�r�X
            services.AddSingleton<AutoTool.Bootstrap.IApplicationBootstrapper, AutoTool.Bootstrap.ApplicationBootstrapper>();

            // ViewModel�o�^�i�V���O���g���j - �W��MVVM�����ɓ���
            services.AddSingleton<EditPanelViewModel>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<FavoritePanelViewModel>();
            services.AddSingleton<ListPanelViewModel>();

            // ���f���o�^
            services.AddTransient<CommandList>();
            services.AddTransient<BasicCommandItem>();

            // JSON�ݒ�o�^
            services.AddSingleton<JsonSerializerOptions>(provider =>
            {
                return new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };
            });

            // CommandListItem�t�@�N�g���[�p�^�[��
            services.AddSingleton<ICommandListItemFactory, CommandListItemFactory>();

            // JsonSerializerHelper�Ƀ��K�[��ݒ�i�z�Q�Ƃ�����邽�ߏ��������ɏ����j
            services.AddSingleton<Action<IServiceProvider>>(provider =>
            {
                return (sp) =>
                {
                    try
                    {
                        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                        var logger = loggerFactory.CreateLogger("JsonSerializerHelper");
                        JsonSerializerHelper.SetLogger(logger);
                        
                        // CommandFactory�ɂ�ServiceProvider��ݒ�
                        AutoTool.Command.Class.CommandFactory.SetServiceProvider(sp);
                    }
                    catch (Exception ex)
                    {
                        // �t�H�[���o�b�N: �R���\�[���ɏo��
                        Console.WriteLine($"JsonSerializerHelper�������G���[: {ex.Message}");
                    }
                };
            });

            return services;
        }
    }

    /// <summary>
    /// CommandListItem�t�@�N�g���[�C���^�[�t�F�[�X
    /// </summary>
    public interface ICommandListItemFactory
    {
        ICommandListItem? CreateItem(string itemType);
    }

    /// <summary>
    /// CommandListItem�t�@�N�g���[����
    /// </summary>
    public class CommandListItemFactory : ICommandListItemFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CommandListItemFactory> _logger;

        public CommandListItemFactory(IServiceProvider serviceProvider, ILogger<CommandListItemFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public ICommandListItem? CreateItem(string itemType)
        {
            try
            {
                _logger.LogDebug("CommandListItemFactory.CreateItem�J�n: {ItemType}", itemType);
                
                // CommandRegistry����^�}�b�s���O���擾
                var itemTypes = CommandRegistry.GetTypeMapping();
                if (itemTypes.TryGetValue(itemType, out var type))
                {
                    _logger.LogDebug("CommandRegistry����^�擾: {Type}", type.Name);
                    
                    // DI�R���e�i����C���X�^���X���擾�����s
                    var serviceInstance = _serviceProvider.GetService(type);
                    if (serviceInstance is ICommandListItem item)
                    {
                        item.ItemType = itemType;
                        item.IsEnable = true;
                        
                        _logger.LogDebug("DI�R���e�i�ō쐬����: {ActualType}", item.GetType().Name);
                        return item;
                    }
                    
                    // DI�Ŏ擾�ł��Ȃ��ꍇ��Activator�ō쐬
                    if (Activator.CreateInstance(type) is ICommandListItem fallbackItem)
                    {
                        fallbackItem.ItemType = itemType;
                        fallbackItem.IsEnable = true;
                        
                        _logger.LogDebug("Activator�ō쐬����: {ActualType}", fallbackItem.GetType().Name);
                        return fallbackItem;
                    }
                }

                _logger.LogWarning("CommandRegistry�ō쐬���s�ABasicCommandItem�ő��: {ItemType}", itemType);

                // �ŏI�t�H�[���o�b�N�FBasicCommandItem
                var basicItem = _serviceProvider.GetService<BasicCommandItem>();
                if (basicItem == null)
                {
                    basicItem = new BasicCommandItem();
                    _logger.LogWarning("BasicCommandItem��DI����擾�ł��Ȃ��������߁A���ڍ쐬���܂���");
                }
                
                basicItem.ItemType = itemType;
                basicItem.IsEnable = true;
                
                _logger.LogDebug("BasicCommandItem�ō쐬����");
                return basicItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CommandListItemFactory.CreateItem���ɃG���[����: {ItemType}", itemType);
                
                // �ً}�t�H�[���o�b�N
                return new BasicCommandItem 
                { 
                    ItemType = itemType, 
                    IsEnable = true
                };
            }
        }
    }
}