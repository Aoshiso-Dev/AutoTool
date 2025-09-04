using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoTool.Services.UI
{
    /// <summary>
    /// ViewとViewModelの依存性注入を管理するサービス
    /// </summary>
    public interface IDataContextLocator
    {
        /// <summary>
        /// 指定されたViewTypeに対応するViewModelを取得
        /// </summary>
        TViewModel GetViewModelForView<TView, TViewModel>() 
            where TView : class 
            where TViewModel : class;

        /// <summary>
        /// ViewModelの型から直接取得
        /// </summary>
        TViewModel GetViewModel<TViewModel>() where TViewModel : class;

        /// <summary>
        /// ViewとViewModelのペアを設定
        /// </summary>
        void ConfigureViewViewModel<TView, TViewModel>() 
            where TView : class 
            where TViewModel : class;
    }

    /// <summary>
    /// DataContextLocatorの実装
    /// </summary>
    public class DataContextLocator : IDataContextLocator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DataContextLocator> _logger;

        public DataContextLocator(IServiceProvider serviceProvider, ILogger<DataContextLocator> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public TViewModel GetViewModelForView<TView, TViewModel>() 
            where TView : class 
            where TViewModel : class
        {
            try
            {
                _logger.LogDebug("ViewModelを取得中: {ViewType} -> {ViewModelType}", 
                    typeof(TView).Name, typeof(TViewModel).Name);

                var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
                
                _logger.LogDebug("ViewModel取得成功: {ViewModelType}", typeof(TViewModel).Name);
                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ViewModel取得に失敗: {ViewType} -> {ViewModelType}", 
                    typeof(TView).Name, typeof(TViewModel).Name);
                throw;
            }
        }

        public TViewModel GetViewModel<TViewModel>() where TViewModel : class
        {
            try
            {
                _logger.LogDebug("ViewModelを直接取得中: {ViewModelType}", typeof(TViewModel).Name);

                var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
                
                _logger.LogDebug("ViewModel直接取得成功: {ViewModelType}", typeof(TViewModel).Name);
                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ViewModel直接取得に失敗: {ViewModelType}", typeof(TViewModel).Name);
                throw;
            }
        }

        public void ConfigureViewViewModel<TView, TViewModel>() 
            where TView : class 
            where TViewModel : class
        {
            _logger.LogInformation("View-ViewModel関連付けを設定: {ViewType} <-> {ViewModelType}",
                typeof(TView).Name, typeof(TViewModel).Name);
        }
    }
}