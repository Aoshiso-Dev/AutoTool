using System.Windows.Controls;
using AutoTool.ViewModel.Panels;

namespace AutoTool.View.Panels
{
    /// <summary>
    /// ListPanelView.xaml の相互作用ロジック（DI対応）
    /// </summary>
    public partial class ListPanelView : System.Windows.Controls.UserControl
    {
        public ListPanelView()
        {
            InitializeComponent();
        }

        // DIコンストラクタ（主にテストのため）
        public ListPanelView(ListPanelViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}