using System.Windows.Controls;
using AutoTool.ViewModel.Panels;

namespace AutoTool.View.Panels
{
    /// <summary>
    /// ListPanelView.xaml の相互作用ロジック（DI対応）
    /// </summary>
    public partial class ListPanelView : UserControl
    {
        public ListPanelView()
        {
            InitializeComponent();
        }

        // レガシーサポート用コンストラクタ（段階的移行のため）
        public ListPanelView(ListPanelViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}