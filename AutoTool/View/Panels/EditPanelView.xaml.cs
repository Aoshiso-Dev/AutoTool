using System;
using System.Windows.Controls;
using AutoTool.ViewModel.Panels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoTool.View.Panels
{
    /// <summary>
    /// EditPanelView.xaml の相互作用ロジック（DI対応）
    /// </summary>
    public partial class EditPanelView : System.Windows.Controls.UserControl
    {
        private ILogger<EditPanelView>? _logger;

        public EditPanelView()
        {
            InitializeComponent();
            
            // DataContextが設定されたときのイベント
            DataContextChanged += EditPanelView_DataContextChanged;
            Loaded += EditPanelView_Loaded;
        }

        private void EditPanelView_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            try
            {
                if (_logger == null)
                {
                    // ロガーを遅延取得
                    if (System.Windows.Application.Current is App app && app.Services != null)
                    {
                        _logger = app.Services.GetService<ILogger<EditPanelView>>();
                    }
                }

                _logger?.LogDebug("EditPanelView DataContextChanged: {OldValue} -> {NewValue}",
                    e.OldValue?.GetType().FullName ?? "null",
                    e.NewValue?.GetType().FullName ?? "null");

                if (e.NewValue is EditPanelViewModel editVM)
                {
                    _logger?.LogInformation("EditPanelViewModelがDataContextに設定されました: {TypeName}", editVM.GetType().FullName);
                    
                    // プロパティ診断テストを実行
                    editVM.DiagnosticProperties();
                }
                else if (e.NewValue != null)
                {
                    _logger?.LogWarning("期待されるEditPanelViewModel以外の型がDataContextに設定されました: {TypeName}", e.NewValue.GetType().FullName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "DataContext変更処理中にエラー");
            }
        }

        private void EditPanelView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (_logger == null)
                {
                    // ロガーを遅延取得
                    if (System.Windows.Application.Current is App app && app.Services != null)
                    {
                        _logger = app.Services.GetService<ILogger<EditPanelView>>();
                    }
                }

                _logger?.LogDebug("EditPanelView Loaded - DataContext: {DataContextType}",
                    DataContext?.GetType().FullName ?? "null");

                if (DataContext is EditPanelViewModel editVM)
                {
                    _logger?.LogInformation("EditPanelViewが正常にロードされました - ViewModelバインディング確認済み");
                }
                else
                {
                    _logger?.LogWarning("EditPanelView - DataContextが正しく設定されていません: {ActualType}",
                        DataContext?.GetType().FullName ?? "null");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "EditPanelView Loaded処理中にエラー");
            }
        }
    }
}