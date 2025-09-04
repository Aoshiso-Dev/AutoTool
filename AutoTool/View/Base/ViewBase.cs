using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.Services.UI;

namespace AutoTool.View.Base
{
    /// <summary>
    /// DI対応のView基底クラス
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
                Logger?.LogError(ex, "View DI初期化中にエラーが発生: {ViewType}", GetType().Name);
            }
        }

        private void InitializeDI()
        {
            if (Application.Current is App app && app.Services != null)
            {
                ServiceProvider = app.Services;
                Logger = ServiceProvider.GetService<ILogger<DIViewBase>>();
                DataContextLocator = ServiceProvider.GetService<IDataContextLocator>();

                Logger?.LogDebug("View DI初期化完了: {ViewType}", GetType().Name);
            }
            else
            {
                throw new InvalidOperationException("DIコンテナが利用できません");
            }
        }

        /// <summary>
        /// DIコンテナ初期化後に呼ばれる（派生クラスでオーバーライド）
        /// </summary>
        protected virtual void OnDIInitialized()
        {
            // 派生クラスでViewModelの設定等を行う
        }

        /// <summary>
        /// ViewModelを自動設定するヘルパーメソッド
        /// </summary>
        protected void SetViewModel<TViewModel>() where TViewModel : class
        {
            if (DataContextLocator != null)
            {
                try
                {
                    var viewModel = DataContextLocator.GetViewModel<TViewModel>();
                    DataContext = viewModel;
                    Logger?.LogDebug("ViewModel自動設定完了: {ViewType} -> {ViewModelType}", 
                        GetType().Name, typeof(TViewModel).Name);
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "ViewModel自動設定に失敗: {ViewType} -> {ViewModelType}", 
                        GetType().Name, typeof(TViewModel).Name);
                }
            }
        }

        /// <summary>
        /// サービスを取得するヘルパーメソッド
        /// </summary>
        protected T? GetService<T>() where T : class
        {
            return ServiceProvider?.GetService<T>();
        }

        /// <summary>
        /// 必須サービスを取得するヘルパーメソッド
        /// </summary>
        protected T GetRequiredService<T>() where T : class
        {
            if (ServiceProvider == null)
                throw new InvalidOperationException("ServiceProvider が初期化されていません");

            return ServiceProvider.GetRequiredService<T>();
        }
    }

    /// <summary>
    /// DI対応のWindow基底クラス
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
                Logger?.LogError(ex, "Window DI初期化中にエラーが発生: {WindowType}", GetType().Name);
            }
        }

        private void InitializeDI()
        {
            if (Application.Current is App app && app.Services != null)
            {
                ServiceProvider = app.Services;
                Logger = ServiceProvider.GetService<ILogger<DIWindowBase>>();
                DataContextLocator = ServiceProvider.GetService<IDataContextLocator>();

                Logger?.LogDebug("Window DI初期化完了: {WindowType}", GetType().Name);
            }
            else
            {
                throw new InvalidOperationException("DIコンテナが利用できません");
            }
        }

        /// <summary>
        /// DIコンテナ初期化後に呼ばれる（派生クラスでオーバーライド）
        /// </summary>
        protected virtual void OnDIInitialized()
        {
            // 派生クラスでViewModelの設定等を行う
        }

        /// <summary>
        /// ViewModelを自動設定するヘルパーメソッド
        /// </summary>
        protected void SetViewModel<TViewModel>() where TViewModel : class
        {
            if (DataContextLocator != null)
            {
                try
                {
                    var viewModel = DataContextLocator.GetViewModel<TViewModel>();
                    DataContext = viewModel;
                    Logger?.LogDebug("ViewModel自動設定完了: {WindowType} -> {ViewModelType}", 
                        GetType().Name, typeof(TViewModel).Name);
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "ViewModel自動設定に失敗: {WindowType} -> {ViewModelType}", 
                        GetType().Name, typeof(TViewModel).Name);
                }
            }
        }

        /// <summary>
        /// サービスを取得するヘルパーメソッド
        /// </summary>
        protected T? GetService<T>() where T : class
        {
            return ServiceProvider?.GetService<T>();
        }

        /// <summary>
        /// 必須サービスを取得するヘルパーメソッド
        /// </summary>
        protected T GetRequiredService<T>() where T : class
        {
            if (ServiceProvider == null)
                throw new InvalidOperationException("ServiceProvider が初期化されていません");

            return ServiceProvider.GetRequiredService<T>();
        }
    }
}