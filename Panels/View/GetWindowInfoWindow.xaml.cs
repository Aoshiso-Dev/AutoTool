using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Drawing;

using WindowHelper;
using MouseHelper;

namespace Panels.View
{
    public partial class GetWindowInfoWindow : Window
    {
        public string WindowTitle { get; set; } = string.Empty;
        public string WindowClassName { get; set; } = string.Empty;

        public GetWindowInfoWindow()
        {
            InitializeComponent();

            // 現在のマウスカーソル位置を取得
            var _screenCurrentPoint = MouseHelper.Input.GetCursorPosition();

            // ウィンドウの位置をマウスポインタの位置に変更
            this.Left = _screenCurrentPoint.X + 5;
            this.Top = _screenCurrentPoint.Y + 5;

            MouseHelper.Event.LButtonDown += OnMouseDown;
            MouseHelper.Event.MouseMove += OnMouseMove;
            MouseHelper.Event.LButtonUp += OnMouseUp;

            MouseHelper.Event.StartHook();
        }

        private void OnMouseDown(object? sender, MouseHelper.Event.MouseEventArgs e)
        {

        }

        private void OnMouseMove(object? sender, MouseHelper.Event.MouseEventArgs e)
        {
            // 現在のマウスカーソル位置を取得
            var _screenCurrentPoint = MouseHelper.Input.GetCursorPosition();

            // ウィンドウの位置をマウスポインタの位置に変更
            this.Left = _screenCurrentPoint.X + 5;
            this.Top = _screenCurrentPoint.Y - this.Height - 5;

            // 直下のウィンドウ情報を取得
            TextBlock_WindowTitle.Text = WindowHelper.Info.GetWindowTitle(_screenCurrentPoint);
            TextBlock_WindowClassName.Text = WindowHelper.Info.GetWindowClassName(_screenCurrentPoint);
        }

        private void OnMouseUp(object? sender, MouseHelper.Event.MouseEventArgs e)
        {
            MouseHelper.Event.StopHook();

            MouseHelper.Event.LButtonDown -= OnMouseDown;
            MouseHelper.Event.MouseMove -= OnMouseMove;
            MouseHelper.Event.LButtonUp -= OnMouseUp;

            WindowTitle = TextBlock_WindowTitle.Text;
            WindowClassName = TextBlock_WindowClassName.Text;

            this.DialogResult = true;
            this.Close();
        }
    }

}