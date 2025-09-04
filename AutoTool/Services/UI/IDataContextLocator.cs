using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoTool.Services.UI
{
    /// <summary>
    /// View��ViewModel�̈ˑ����������Ǘ�����T�[�r�X
    /// </summary>
    public interface IDataContextLocator
    {
        /// <summary>
        /// �w�肳�ꂽViewType�ɑΉ�����ViewModel���擾
        /// </summary>
        TViewModel GetViewModelForView<TView, TViewModel>() 
            where TView : class 
            where TViewModel : class;

        /// <summary>
        /// ViewModel�̌^���璼�ڎ擾
        /// </summary>
        TViewModel GetViewModel<TViewModel>() where TViewModel : class;

        /// <summary>
        /// View��ViewModel�̃y�A��ݒ�
        /// </summary>
        void ConfigureViewViewModel<TView, TViewModel>() 
            where TView : class 
            where TViewModel : class;
    }

    /// <summary>
    /// DataContextLocator�̎���
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
                _logger.LogDebug("ViewModel���擾��: {ViewType} -> {ViewModelType}", 
                    typeof(TView).Name, typeof(TViewModel).Name);

                var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
                
                _logger.LogDebug("ViewModel�擾����: {ViewModelType}", typeof(TViewModel).Name);
                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ViewModel�擾�Ɏ��s: {ViewType} -> {ViewModelType}", 
                    typeof(TView).Name, typeof(TViewModel).Name);
                throw;
            }
        }

        public TViewModel GetViewModel<TViewModel>() where TViewModel : class
        {
            try
            {
                _logger.LogDebug("ViewModel�𒼐ڎ擾��: {ViewModelType}", typeof(TViewModel).Name);

                var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
                
                _logger.LogDebug("ViewModel���ڎ擾����: {ViewModelType}", typeof(TViewModel).Name);
                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ViewModel���ڎ擾�Ɏ��s: {ViewModelType}", typeof(TViewModel).Name);
                throw;
            }
        }

        public void ConfigureViewViewModel<TView, TViewModel>() 
            where TView : class 
            where TViewModel : class
        {
            _logger.LogInformation("View-ViewModel�֘A�t����ݒ�: {ViewType} <-> {ViewModelType}",
                typeof(TView).Name, typeof(TViewModel).Name);
        }
    }
}