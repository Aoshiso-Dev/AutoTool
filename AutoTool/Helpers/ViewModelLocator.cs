using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.Services.UI;

namespace AutoTool.Helpers
{
    /// <summary>
    /// XAMLからViewModelを自動バインドするためのAttached Property
    /// </summary>
    public static class ViewModelLocator
    {
        private static IServiceProvider? _serviceProvider;
        private static ILogger? _logger;

        /// <summary>
        /// サービスプロバイダーを設定
        /// </summary>
        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("ViewModelLocator");
        }

        #region AutoWireViewModel Attached Property

        public static readonly DependencyProperty AutoWireViewModelProperty =
            DependencyProperty.RegisterAttached(
                "AutoWireViewModel",
                typeof(bool),
                typeof(ViewModelLocator),
                new PropertyMetadata(false, OnAutoWireViewModelChanged));

        public static bool GetAutoWireViewModel(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoWireViewModelProperty);
        }

        public static void SetAutoWireViewModel(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoWireViewModelProperty, value);
        }

        private static void OnAutoWireViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                AutoWireViewModelForView(d);
            }
        }

        #endregion

        #region ViewModelType Attached Property

        public static readonly DependencyProperty ViewModelTypeProperty =
            DependencyProperty.RegisterAttached(
                "ViewModelType",
                typeof(Type),
                typeof(ViewModelLocator),
                new PropertyMetadata(null, OnViewModelTypeChanged));

        public static Type GetViewModelType(DependencyObject obj)
        {
            return (Type)obj.GetValue(ViewModelTypeProperty);
        }

        public static void SetViewModelType(DependencyObject obj, Type value)
        {
            obj.SetValue(ViewModelTypeProperty, value);
        }

        private static void OnViewModelTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is Type viewModelType)
            {
                AutoWireViewModelByType(d, viewModelType);
            }
        }

        #endregion

        /// <summary>
        /// Viewに対応するViewModelを自動ワイヤリング
        /// </summary>
        private static void AutoWireViewModelForView(DependencyObject view)
        {
            if (_serviceProvider == null || view == null) return;

            try
            {
                var viewType = view.GetType();
                var viewModelTypeName = viewType.Name.Replace("View", "ViewModel");
                var viewModelType = FindViewModelType(viewModelTypeName);

                if (viewModelType != null)
                {
                    AutoWireViewModelByType(view, viewModelType);
                }
                else
                {
                    _logger?.LogWarning("ViewModel not found for view: {ViewType}", viewType.Name);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Auto-wiring failed for view: {ViewType}", view.GetType().Name);
            }
        }

        /// <summary>
        /// 指定された型のViewModelを自動ワイヤリング
        /// </summary>
        private static void AutoWireViewModelByType(DependencyObject view, Type viewModelType)
        {
            if (_serviceProvider == null || view == null || viewModelType == null) return;

            try
            {
                var viewModel = _serviceProvider.GetRequiredService(viewModelType);
                
                if (view is FrameworkElement frameworkElement)
                {
                    frameworkElement.DataContext = viewModel;
                    _logger?.LogDebug("ViewModel auto-wired: {ViewType} -> {ViewModelType}", 
                        view.GetType().Name, viewModelType.Name);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to auto-wire ViewModel: {ViewType} -> {ViewModelType}", 
                    view.GetType().Name, viewModelType.Name);
            }
        }

        /// <summary>
        /// ViewModel型を検索
        /// </summary>
        private static Type? FindViewModelType(string viewModelTypeName)
        {
            // AutoTool.ViewModel名前空間を検索
            var assembly = typeof(ViewModelLocator).Assembly;
            var fullTypeName = $"AutoTool.ViewModel.{viewModelTypeName}";
            var type = assembly.GetType(fullTypeName);

            if (type != null) return type;

            // AutoTool.ViewModel.Panels名前空間を検索
            fullTypeName = $"AutoTool.ViewModel.Panels.{viewModelTypeName}";
            type = assembly.GetType(fullTypeName);

            return type;
        }

        /// <summary>
        /// 特定のViewModelを手動で設定
        /// </summary>
        public static void SetViewModel<TViewModel>(FrameworkElement view) where TViewModel : class
        {
            if (_serviceProvider == null) return;

            try
            {
                var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
                view.DataContext = viewModel;
                _logger?.LogDebug("ViewModel manually set: {ViewType} -> {ViewModelType}", 
                    view.GetType().Name, typeof(TViewModel).Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to manually set ViewModel: {ViewType} -> {ViewModelType}", 
                    view.GetType().Name, typeof(TViewModel).Name);
            }
        }
    }
}