using System.Windows.Controls;
using AutoTool.ViewModel.Panels;

namespace AutoTool.View.Panels
{
    /// <summary>
    /// ListPanelView.xaml �̑��ݍ�p���W�b�N�iDI�Ή��j
    /// </summary>
    public partial class ListPanelView : System.Windows.Controls.UserControl
    {
        public ListPanelView()
        {
            InitializeComponent();
        }

        // DI�R���X�g���N�^�i��Ƀe�X�g�̂��߁j
        public ListPanelView(ListPanelViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}