using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AutoTool.Desktop.ViewModels;

namespace AutoTool.Desktop.Views.Parts
{
    /// <summary>
    /// ButtonPanel.xaml の相互作用ロジック
    /// </summary>
    public partial class ButtonPanel : UserControl
    {
        public ButtonPanel()
        {
            InitializeComponent();
        }

        public ButtonPanel(ButtonPanelViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}
