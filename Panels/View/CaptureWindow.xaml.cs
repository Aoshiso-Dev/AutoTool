using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Panels.View
{
    public partial class CaptureWindow : Window
    {
        private Point _startPoint;
        private Rectangle _selectionRectangle;

        // 呼び出し元から設定するためのファイル名プロパティ
        public string FileName { get; set; } = string.Empty;
        public Rect SelectedRegion { get; set; } = Rect.Empty;


        public CaptureWindow()
        {
            InitializeComponent();

            _selectionRectangle = new Rectangle
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                Visibility = Visibility.Hidden
            };

            MainCanvas.Children.Add(_selectionRectangle);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _startPoint = e.GetPosition(MainCanvas);
                _selectionRectangle.Visibility = Visibility.Visible;

                Canvas.SetLeft(_selectionRectangle, _startPoint.X);
                Canvas.SetTop(_selectionRectangle, _startPoint.Y);
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_selectionRectangle.Visibility == Visibility.Visible)
            {
                var currentPoint = e.GetPosition(MainCanvas);

                var x = Math.Min(currentPoint.X, _startPoint.X);
                var y = Math.Min(currentPoint.Y, _startPoint.Y);

                var width = Math.Abs(currentPoint.X - _startPoint.X);
                var height = Math.Abs(currentPoint.Y - _startPoint.Y);

                _selectionRectangle.Width = width;
                _selectionRectangle.Height = height;

                Canvas.SetLeft(_selectionRectangle, x);
                Canvas.SetTop(_selectionRectangle, y);
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_selectionRectangle.Visibility == Visibility.Visible)
            {
                // キャプチャ領域の決定
                SelectedRegion = new Rect(
                    Canvas.GetLeft(_selectionRectangle),
                    Canvas.GetTop(_selectionRectangle),
                    _selectionRectangle.Width,
                    _selectionRectangle.Height);



                // 選択領域をキャプチャ
                //var capturedMat = ScreenCaptureHelper.CaptureRegion(selectedRegion);

                // 指定されたファイル名で保存
                //ScreenCaptureHelper.SaveCapture(capturedMat, $"{FileName}");

                this.DialogResult = true; // ダイアログ結果を設定してウィンドウを閉じる
                this.Close();
            }
        }
    }

}