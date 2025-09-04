using System;
using System.Windows.Controls;
using AutoTool.ViewModel.Panels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoTool.View.Panels
{
    /// <summary>
    /// EditPanelView.xaml �̑��ݍ�p���W�b�N�iDI�Ή��j
    /// </summary>
    public partial class EditPanelView : System.Windows.Controls.UserControl
    {
        private ILogger<EditPanelView>? _logger;

        public EditPanelView()
        {
            InitializeComponent();
            
            // DataContext���ݒ肳�ꂽ�Ƃ��̃C�x���g
            DataContextChanged += EditPanelView_DataContextChanged;
            Loaded += EditPanelView_Loaded;
        }

        private void EditPanelView_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            try
            {
                if (_logger == null)
                {
                    // ���K�[��x���擾
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
                    _logger?.LogInformation("EditPanelViewModel��DataContext�ɐݒ肳��܂���: {TypeName}", editVM.GetType().FullName);
                    
                    // �v���p�e�B�f�f�e�X�g�����s
                    editVM.DiagnosticProperties();
                }
                else if (e.NewValue != null)
                {
                    _logger?.LogWarning("���҂����EditPanelViewModel�ȊO�̌^��DataContext�ɐݒ肳��܂���: {TypeName}", e.NewValue.GetType().FullName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "DataContext�ύX�������ɃG���[");
            }
        }

        private void EditPanelView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (_logger == null)
                {
                    // ���K�[��x���擾
                    if (System.Windows.Application.Current is App app && app.Services != null)
                    {
                        _logger = app.Services.GetService<ILogger<EditPanelView>>();
                    }
                }

                _logger?.LogDebug("EditPanelView Loaded - DataContext: {DataContextType}",
                    DataContext?.GetType().FullName ?? "null");

                if (DataContext is EditPanelViewModel editVM)
                {
                    _logger?.LogInformation("EditPanelView������Ƀ��[�h����܂��� - ViewModel�o�C���f�B���O�m�F�ς�");
                }
                else
                {
                    _logger?.LogWarning("EditPanelView - DataContext���������ݒ肳��Ă��܂���: {ActualType}",
                        DataContext?.GetType().FullName ?? "null");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "EditPanelView Loaded�������ɃG���[");
            }
        }
    }
}