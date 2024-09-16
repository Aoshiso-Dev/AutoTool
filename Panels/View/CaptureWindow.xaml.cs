using InputHelper;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Drawing;

namespace Panels.View
{
    public partial class CaptureWindow : Window
    {
        private System.Drawing.Point _screenStartPoint;
        private System.Drawing.Point _screenCurrentPoint;
        private System.Windows.Point _canvasStartPoint;
        private System.Windows.Point _canvasCurrentPoint;
        private System.Windows.Shapes.Rectangle _canvasSelectionRectangle;

        public int Mode { get; set; } = 0;
        public Rect SelectedRegion { get; set; } = Rect.Empty;
        public System.Drawing.Point SelectedPoint { get; set; } = new System.Drawing.Point();


        public CaptureWindow()
        {
            InitializeComponent();

            _canvasSelectionRectangle = new System.Windows.Shapes.Rectangle
            {
                Fill = new SolidColorBrush(Colors.LightBlue),
                Stroke = new SolidColorBrush(Colors.Blue),
                StrokeThickness = 2,
                Visibility = Visibility.Hidden
            };

            MainCanvas.Children.Add(_canvasSelectionRectangle);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mode == 0)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    _screenStartPoint = MouseControlHelper.GetCursorPosition();
                    _canvasStartPoint = PointToScreen(e.GetPosition(this));

                    _canvasSelectionRectangle.Visibility = Visibility.Visible;

                    Canvas.SetLeft(_canvasSelectionRectangle, _canvasStartPoint.X);
                    Canvas.SetTop(_canvasSelectionRectangle, _canvasStartPoint.Y);
                }
            }
            else if (Mode == 1)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    SelectedPoint = MouseControlHelper.GetCursorPosition();
                    this.DialogResult = true;
                    this.Close();
                }
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (Mode == 0 && e.LeftButton == MouseButtonState.Pressed)
            {
                if (_canvasSelectionRectangle.Visibility == Visibility.Visible)
                {
                    // 現在のマウスカーソル位置を取得
                    _screenCurrentPoint = MouseControlHelper.GetCursorPosition();

                    // キャンバス更新：現在のキャンバス座標を取得
                    _canvasCurrentPoint = PointToScreen(e.GetPosition(this));

                    // キャンバス更新：開始位置と現在位置から選択範囲を計算
                    var canvasX = Math.Min(_canvasCurrentPoint.X - Left, _canvasStartPoint.X - Left);
                    var canvasY = Math.Min(_canvasCurrentPoint.Y - Top, _canvasStartPoint.Y - Top);
                    var canvasWidth = Math.Abs(_canvasCurrentPoint.X - _canvasStartPoint.X);
                    var canvasHeight = Math.Abs(_canvasCurrentPoint.Y - _canvasStartPoint.Y);

                    // キャンバス更新：選択矩形の位置とサイズをキャンバス座標で設定
                    Canvas.SetLeft(_canvasSelectionRectangle, canvasX);
                    Canvas.SetTop(_canvasSelectionRectangle, canvasY);
                    _canvasSelectionRectangle.Width = canvasWidth;
                    _canvasSelectionRectangle.Height = canvasHeight;
                }
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Mode == 0 && e.LeftButton == MouseButtonState.Released)
            {
                if (_canvasSelectionRectangle.Visibility == Visibility.Visible)
                {
                    SelectedRegion = new Rect(
                        Math.Min(_screenStartPoint.X, _screenCurrentPoint.X),
                        Math.Min(_screenStartPoint.Y, _screenCurrentPoint.Y),
                        Math.Abs(_screenCurrentPoint.X - _screenStartPoint.X),
                        Math.Abs(_screenCurrentPoint.Y - _screenStartPoint.Y)
                    );

                    this.DialogResult = !SelectedRegion.IsEmpty;
                    this.Close();
                }
            }
        }
    }

}