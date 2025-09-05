using System;
using System.Windows;
using System.Windows.Controls;
using AutoTool.ViewModel.Panels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoTool.View.Panels
{
    /// <summary>
    /// ButtonPanelView.xaml �̑��ݍ�p���W�b�N
    /// </summary>
    public partial class ButtonPanelView : System.Windows.Controls.UserControl
    {
        private ILogger<ButtonPanelView>? _logger;
        private ButtonPanelViewModel? ViewModel => DataContext as ButtonPanelViewModel;

        public ButtonPanelView()
        {
            InitializeComponent();
            
            // DataContext ���ݒ肳�ꂽ�Ƃ��̃C�x���g
            DataContextChanged += ButtonPanelView_DataContextChanged;
            Loaded += ButtonPanelView_Loaded;
        }

        private void ButtonPanelView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                if (_logger == null)
                {
                    // ���K�[���擾
                    if (System.Windows.Application.Current is App app && app.Services != null)
                    {
                        _logger = app.Services.GetService<ILogger<ButtonPanelView>>();
                    }
                }

                _logger?.LogDebug("ButtonPanelView DataContextChanged: {OldValue} -> {NewValue}",
                    e.OldValue?.GetType().FullName ?? "null",
                    e.NewValue?.GetType().FullName ?? "null");

                if (e.NewValue is ButtonPanelViewModel buttonVM)
                {
                    _logger?.LogInformation("ButtonPanelViewModel��DataContext�ɐݒ肳��܂���: {TypeName}", buttonVM.GetType().FullName);
                }
                else if (e.NewValue != null)
                {
                    _logger?.LogWarning("���҂���ButtonPanelViewModel�ȊO�̌^��DataContext�ɐݒ肳��܂���: {TypeName}", e.NewValue.GetType().FullName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "DataContext�ύX�������ɃG���[");
            }
        }

        private void ButtonPanelView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_logger == null)
                {
                    // ���K�[���Ď擾
                    if (System.Windows.Application.Current is App app && app.Services != null)
                    {
                        _logger = app.Services.GetService<ILogger<ButtonPanelView>>();
                    }
                }

                _logger?.LogDebug("ButtonPanelView Loaded: DataContext = {DataContext}",
                    DataContext?.GetType().FullName ?? "null");

                if (ViewModel != null)
                {
                    _logger?.LogInformation("ButtonPanelView�ǂݍ��݊���: ViewModel�����p�\");
                    
                    // ����������������Ύ��s
                    ViewModel.Prepare();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Loaded�������ɃG���[");
            }
        }

        /// <summary>
        /// ���v�\���{�^���N���b�N�C�x���g
        /// </summary>
        private void ShowStatsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel != null)
                {
                    var stats = ViewModel.GetCommandTypeStats();
                    
                    var statsMessage = $"�R�}���h�^�C�v���v:\n\n" +
                        $"���^�C�v��: {stats.TotalTypes}\n" +
                        $"�ŋߎg�p: {stats.RecentCount}\n" +
                        $"���C�ɓ���: {stats.FavoriteCount}\n\n" +
                        "�J�e�S����:\n";
                    
                    foreach (var categoryStats in stats.CategoryStats)
                    {
                        statsMessage += $"  {categoryStats.Key}: {categoryStats.Value}��\n";
                    }
                    
                    System.Windows.MessageBox.Show(statsMessage, "�R�}���h���v", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    _logger?.LogInformation("�R�}���h���v��\�����܂���");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "���v�\�����ɃG���[");
                System.Windows.MessageBox.Show($"���v�\�����ɃG���[���������܂���: {ex.Message}", 
                    "���v�\���G���[", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}