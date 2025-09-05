using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.Services.Plugin;
using AutoTool.Services.Mouse;
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
            services.AddSingleton<CommandListService>();

            // Plugin Services
            services.AddSingleton<IPluginService, PluginService>();

            // Theme Services
            services.AddSingleton<IEnhancedThemeService, EnhancedThemeService>();

            // UI Services
            services.AddTransient<IMainWindowMenuService, MainWindowMenuService>();
            services.AddTransient<IMainWindowButtonService, MainWindowButtonService>();

            // Mouse Services
            services.AddSingleton<IMouseService, MouseService>();

            // Capture Services
            services.AddSingleton<ICaptureService, CaptureService>();

            // ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<ListPanelViewModel>();
            services.AddTransient<EditPanelViewModel>();

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

                // 1. ���I�V�X�e����UniversalCommandItem���쐬
                try
                {
                    var universalItem = DirectCommandRegistry.CreateUniversalItem(itemType);
                    if (universalItem != null)
                    {
                        _logger.LogDebug("���I�V�X�e����UniversalCommandItem�쐬����: {ItemType}", itemType);
                        return universalItem;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "���I�V�X�e���ł̍쐬���s�A�]���V�X�e���Ƀt�H�[���o�b�N: {ItemType}", itemType);
                }

                // 2. �]����CommandRegistry�i����݊����j
                var itemTypes = CommandRegistry.GetTypeMapping();
                if (itemTypes.TryGetValue(itemType, out var type))
                {
                    _logger.LogDebug("CommandRegistry ����^�擾: {Type}", type.Name);

                    // DI �R���e�i���� �C���X�^���X ���擾�����s
                    var serviceInstance = _serviceProvider.GetService(type);
                    if (serviceInstance is ICommandListItem item)
                    {
                        item.ItemType = itemType;
                        item.IsEnable = true;

                        _logger.LogDebug("DI �R���e�i�ō쐬����: {ActualType}", item.GetType().Name);
                        return item;
                    }

                    // DI �Ŏ擾�ł��Ȃ��ꍇ�� Activator �ō쐬
                    if (Activator.CreateInstance(type) is ICommandListItem fallbackItem)
                    {
                        fallbackItem.ItemType = itemType;
                        fallbackItem.IsEnable = true;

                        _logger.LogDebug("Activator �ō쐬����: {ActualType}", fallbackItem.GetType().Name);
                        return fallbackItem;
                    }
                }

                _logger.LogWarning("CommandRegistry �ō쐬���s�ABasicCommandItem �ő�p: {ItemType}", itemType);

                // 3. �ŏI�t�H�[���o�b�N�FBasicCommandItem
                var basicItem = _serviceProvider.GetService<BasicCommandItem>();
                if (basicItem == null)
                {
                    basicItem = new BasicCommandItem();
                    _logger.LogWarning("BasicCommandItem �� DI ����擾�ł��Ȃ��������߁A���ڍ쐬���܂���");
                }

                basicItem.ItemType = itemType;
                basicItem.IsEnable = true;

                _logger.LogDebug("BasicCommandItem �ō쐬����");
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
}