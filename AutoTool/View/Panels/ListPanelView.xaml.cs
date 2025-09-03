using System.Windows.Controls;
using AutoTool.ViewModel.Panels;

namespace AutoTool.View.Panels
{
    /// <summary>
    /// ListPanelView.xaml の相互作用ロジック
    /// </summary>
    public partial class ListPanelView : UserControl
    {
        public ListPanelView()
        {
            InitializeComponent();
        }

        public ListPanelView(ListPanelViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}