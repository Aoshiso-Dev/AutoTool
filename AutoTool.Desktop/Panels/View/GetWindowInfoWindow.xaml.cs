using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Drawing;
using AutoTool.Commands.Infrastructure;

namespace AutoTool.Panels.View
{
    public partial class GetWindowInfoWindow : Window
    {
        private bool _hookRegistered;

        public string WindowTitle { get; set; } = string.Empty;
        public string WindowClassName { get; set; } = string.Empty;

        public GetWindowInfoWindow()
        {
            InitializeComponent();

            // 現在のマウスカーソル位置を取得
            var _screenCurrentPoint = Win32MouseInterop.GetCursorPosition();

            // ウィンドウの位置をマウスポインタの位置に変更
            this.Left = _screenCurrentPoint.X + 5;
            this.Top = _screenCurrentPoint.Y + 5;

            Win32MouseHookHelper.LButtonDown += OnMouseDown;
            Win32MouseHookHelper.MouseMove += OnMouseMove;
            Win32MouseHookHelper.LButtonUp += OnMouseUp;

            Win32MouseHookHelper.StartHook();
            _hookRegistered = true;
        }

        private void OnMouseDown(object? sender, Win32MouseHookHelper.MouseEventArgs e)
        {

        }

        private void OnMouseMove(object? sender, Win32MouseHookHelper.MouseEventArgs e)
        {
            // 現在のマウスカーソル位置を取得
            var _screenCurrentPoint = Win32MouseInterop.GetCursorPosition();

            // ウィンドウの位置をマウスポインタの位置に変更
            this.Left = _screenCurrentPoint.X + 5;
            this.Top = _screenCurrentPoint.Y - this.Height - 5;

            // 直下のウィンドウ情報を取得
            TextBlock_WindowTitle.Text = Win32WindowInfoHelper.GetWindowTitle(_screenCurrentPoint);
            TextBlock_WindowClassName.Text = Win32WindowInfoHelper.GetWindowClassName(_screenCurrentPoint);
        }

        private void OnMouseUp(object? sender, Win32MouseHookHelper.MouseEventArgs e)
        {
            StopHook();

            WindowTitle = TextBlock_WindowTitle.Text;
            WindowClassName = TextBlock_WindowClassName.Text;

            this.DialogResult = true;
            this.Close();
        }

        private void StopHook()
        {
            if (!_hookRegistered)
            {
                return;
            }

            Win32MouseHookHelper.LButtonDown -= OnMouseDown;
            Win32MouseHookHelper.MouseMove -= OnMouseMove;
            Win32MouseHookHelper.LButtonUp -= OnMouseUp;
            Win32MouseHookHelper.StopHook();
            _hookRegistered = false;
        }

        protected override void OnClosed(EventArgs e)
        {
            StopHook();
            base.OnClosed(e);
        }
    }

}


