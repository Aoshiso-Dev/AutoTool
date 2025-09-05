using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Plugin;
using AutoTool.Services.Mouse;
using AutoTool.ViewModel;
using AutoTool.ViewModel.Panels;
using AutoTool.Model.List.Type;
using AutoTool.Model.CommandDefinition;
using AutoTool.Model.List.Interface;
using AutoTool.Helpers;
using System.Text.Json;
using CommunityToolkit.Mvvm.Messaging;
using System;
using AutoTool.Services.UI;
using AutoTool.Services.Capture;
using AutoTool.Services.Configuration;

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

            // Configuration Services
            services.AddSingleton<IEnhancedConfigurationService, EnhancedConfigurationService>();

            // Core Services
            services.AddSingleton<IRecentFileService, RecentFileService>();
            services.AddSingleton<AutoTool.Command.Interface.IVariableStore, AutoTool.Command.Class.VariableStore>();

            // Plugin Services
            services.AddSingleton<IPluginService, PluginService>();

            // Theme Services
            services.AddSingleton<IEnhancedThemeService, EnhancedThemeService>();

            // UI Services
            services.AddTransient<IMainWindowMenuService, MainWindowMenuService>();
            services.AddTransient<IMainWindowButtonService, MainWindowButtonService>();
            services.AddTransient<IEditPanelPropertyService, EditPanelPropertyService>();
            services.AddTransient<IEditPanelIntegrationService, EditPanelIntegrationService>();

            // Mouse Services
            services.AddSingleton<IMouseService, MouseService>();

            // Capture Services
            services.AddSingleton<ICaptureService, CaptureService>();

            // Window Services
            services.AddSingleton<AutoTool.Services.Window.IWindowInfoService, AutoTool.Services.Window.WindowInfoService>();

            // ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<ListPanelViewModel>();
            services.AddTransient<EditPanelViewModel>();
            services.AddTransient<ButtonPanelViewModel>();

            // Command List Item Factory
            services.AddSingleton<ICommandListItemFactory, CommandListItemFactory>();

            // Dummy services for missing dependencies
            services.AddSingleton<IDataContextLocator, DataContextLocator>();

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
    /// CommandListItem�t�@�N�g���[�����iDirectCommandRegistry����Łj
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

                // DirectCommandRegistry���g�p����UniversalCommandItem���쐬
                try
                {
                    var universalItem = DirectCommandRegistry.CreateUniversalItem(itemType);
                    if (universalItem != null)
                    {
                        _logger.LogDebug("DirectCommandRegistry��UniversalCommandItem�쐬����: {ItemType}", itemType);
                        return universalItem;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "DirectCommandRegistry�ł̍쐬���s�ABasicCommandItem�Ƀt�H�[���o�b�N: {ItemType}", itemType);
                }

                // �t�H�[���o�b�N�FBasicCommandItem
                var basicItem = _serviceProvider.GetService<BasicCommandItem>();
                if (basicItem == null)
                {
                    basicItem = new BasicCommandItem();
                    _logger.LogWarning("BasicCommandItem �� DI ����擾�ł��Ȃ��������߁A���ڍ쐬���܂���");
                }

                basicItem.ItemType = itemType;
                basicItem.IsEnable = true;

                _logger.LogDebug("BasicCommandItem �ō쐬����: {ItemType}", itemType);
                return basicItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CommandListItemFactory.CreateItem ���ɃG���[����: {ItemType}", itemType);

                // �ً}�t�H�[���o�b�N
                return new BasicCommandItem
                {
                    ItemType = itemType,
                    IsEnable = true
                };
            }
        }
    }

    /// <summary>
    /// DataContextLocator�C���^�[�t�F�[�X
    /// </summary>
    public interface IDataContextLocator
    {
        T? GetDataContext<T>() where T : class;
        void SetDataContext<T>(T dataContext) where T : class;
    }

    /// <summary>
    /// DataContextLocator����
    /// </summary>
    public class DataContextLocator : IDataContextLocator
    {
        private readonly Dictionary<Type, object> _dataContexts = new();

        public T? GetDataContext<T>() where T : class
        {
            _dataContexts.TryGetValue(typeof(T), out var dataContext);
            return dataContext as T;
        }

        public void SetDataContext<T>(T dataContext) where T : class
        {
            _dataContexts[typeof(T)] = dataContext;
        }
    }
}