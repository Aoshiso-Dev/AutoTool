using System.Windows.Controls;
using AutoTool.ViewModel.Panels;

namespace AutoTool.View.Panels
{
    /// <summary>
    /// ListPanelView.xaml �̑��ݍ�p���W�b�N�iDI�Ή��j
    /// </summary>
    public partial class ListPanelView : UserControl
    {
        public ListPanelView()
        {
            InitializeComponent();
        }

        // ���K�V�[�T�|�[�g�p�R���X�g���N�^�i�i�K�I�ڍs�̂��߁j
        public ListPanelView(ListPanelViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}