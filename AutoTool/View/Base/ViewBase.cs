using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.Services.UI;

namespace AutoTool.View.Base
{
    /// <summary>
    /// DI�Ή���View���N���X
    /// </summary>
    public abstract class DIViewBase : UserControl
    {
        protected ILogger? Logger { get; private set; }
        protected IServiceProvider? ServiceProvider { get; private set; }
        protected IDataContextLocator? DataContextLocator { get; private set; }

        protected DIViewBase()
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                Loaded += OnViewLoaded;
            }
        }

        private void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                InitializeDI();
                OnDIInitialized();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "View DI���������ɃG���[������: {ViewType}", GetType().Name);
            }
        }

        private void InitializeDI()
        {
            if (Application.Current is App app && app.Services != null)
            {
                ServiceProvider = app.Services;
                Logger = ServiceProvider.GetService<ILogger<DIViewBase>>();
                DataContextLocator = ServiceProvider.GetService<IDataContextLocator>();

                Logger?.LogDebug("View DI����������: {ViewType}", GetType().Name);
            }
            else
            {
                throw new InvalidOperationException("DI�R���e�i�����p�ł��܂���");
            }
        }

        /// <summary>
        /// DI�R���e�i��������ɌĂ΂��i�h���N���X�ŃI�[�o�[���C�h�j
        /// </summary>
        protected virtual void OnDIInitialized()
        {
            // �h���N���X��ViewModel�̐ݒ蓙���s��
        }

        /// <summary>
        /// ViewModel�������ݒ肷��w���p�[���\�b�h
        /// </summary>
        protected void SetViewModel<TViewModel>() where TViewModel : class
        {
            if (DataContextLocator != null)
            {
                try
                {
                    var viewModel = DataContextLocator.GetViewModel<TViewModel>();
                    DataContext = viewModel;
                    Logger?.LogDebug("ViewModel�����ݒ芮��: {ViewType} -> {ViewModelType}", 
                        GetType().Name, typeof(TViewModel).Name);
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "ViewModel�����ݒ�Ɏ��s: {ViewType} -> {ViewModelType}", 
                        GetType().Name, typeof(TViewModel).Name);
                }
            }
        }

        /// <summary>
        /// �T�[�r�X���擾����w���p�[���\�b�h
        /// </summary>
        protected T? GetService<T>() where T : class
        {
            return ServiceProvider?.GetService<T>();
        }

        /// <summary>
        /// �K�{�T�[�r�X���擾����w���p�[���\�b�h
        /// </summary>
        protected T GetRequiredService<T>() where T : class
        {
            if (ServiceProvider == null)
                throw new InvalidOperationException("ServiceProvider ������������Ă��܂���");

            return ServiceProvider.GetRequiredService<T>();
        }
    }

    /// <summary>
    /// DI�Ή���Window���N���X
    /// </summary>
    public abstract class DIWindowBase : Window
    {
        protected ILogger? Logger { get; private set; }
        protected IServiceProvider? ServiceProvider { get; private set; }
        protected IDataContextLocator? DataContextLocator { get; private set; }

        protected DIWindowBase()
        {
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                Loaded += OnWindowLoaded;
            }
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                InitializeDI();
                OnDIInitialized();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Window DI���������ɃG���[������: {WindowType}", GetType().Name);
            }
        }

        private void InitializeDI()
        {
            if (Application.Current is App app && app.Services != null)
            {
                ServiceProvider = app.Services;
                Logger = ServiceProvider.GetService<ILogger<DIWindowBase>>();
                DataContextLocator = ServiceProvider.GetService<IDataContextLocator>();

                Logger?.LogDebug("Window DI����������: {WindowType}", GetType().Name);
            }
            else
            {
                throw new InvalidOperationException("DI�R���e�i�����p�ł��܂���");
            }
        }

        /// <summary>
        /// DI�R���e�i��������ɌĂ΂��i�h���N���X�ŃI�[�o�[���C�h�j
        /// </summary>
        protected virtual void OnDIInitialized()
        {
            // �h���N���X��ViewModel�̐ݒ蓙���s��
        }

        /// <summary>
        /// ViewModel�������ݒ肷��w���p�[���\�b�h
        /// </summary>
        protected void SetViewModel<TViewModel>() where TViewModel : class
        {
            if (DataContextLocator != null)
            {
                try
                {
                    var viewModel = DataContextLocator.GetViewModel<TViewModel>();
                    DataContext = viewModel;
                    Logger?.LogDebug("ViewModel�����ݒ芮��: {WindowType} -> {ViewModelType}", 
                        GetType().Name, typeof(TViewModel).Name);
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "ViewModel�����ݒ�Ɏ��s: {WindowType} -> {ViewModelType}", 
                        GetType().Name, typeof(TViewModel).Name);
                }
            }
        }

        /// <summary>
        /// �T�[�r�X���擾����w���p�[���\�b�h
        /// </summary>
        protected T? GetService<T>() where T : class
        {
            return ServiceProvider?.GetService<T>();
        }

        /// <summary>
        /// �K�{�T�[�r�X���擾����w���p�[���\�b�h
        /// </summary>
        protected T GetRequiredService<T>() where T : class
        {
            if (ServiceProvider == null)
                throw new InvalidOperationException("ServiceProvider ������������Ă��܂���");

            return ServiceProvider.GetRequiredService<T>();
        }
    }
}