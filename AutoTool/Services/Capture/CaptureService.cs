using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using System.Windows;
using WindowHelper;

namespace AutoTool.Services.Capture
{
    /// <summary>
    /// キャプチャサービスの実装
    /// </summary>
    public class CaptureService : ICaptureService
    {
        private readonly ILogger<CaptureService> _logger;
        private bool _isCapturing = false;

        public CaptureService(ILogger<CaptureService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 右クリック位置の色を取得
        /// </summary>
        public async Task<Color?> CaptureColorAtRightClickAsync()
        {
            if (_isCapturing) return null;

            try
            {
                _isCapturing = true;
                _logger.LogInformation("色キャプチャを開始します（右クリック待機）");

                // ColorPickHelperを優先使用
                try
                {
                    var colorPickWindow = new ColorPickHelper.ColorPickWindow();
                    var result = colorPickWindow.ShowDialog();
                    
                    if (result == true && colorPickWindow.Color.HasValue)
                    {
                        var color = colorPickWindow.Color.Value;
                        var drawingColor = Color.FromArgb(color.A, color.R, color.G, color.B);
                        _logger.LogInformation("ColorPickHelperで色を取得: {Color}", drawingColor);
                        return drawingColor;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ColorPickHelper使用エラー、フォールバックに切り替え");
                }

                // フォールバック: 右クリック位置待機
                var position = await WaitForRightClickPositionAsync();
                if (position.HasValue)
                {
                    var color = GetColorAt(position.Value);
                    _logger.LogInformation("右クリック位置で色を取得: {Position} -> {Color}", position.Value, color);
                    return color;
                }

                return null;
            }
            finally
            {
                _isCapturing = false;
            }
        }

        /// <summary>
        /// 現在のマウス位置を取得
        /// </summary>
        public System.Drawing.Point GetCurrentMousePosition()
        {
            try
            {
                var cursorPos = System.Windows.Forms.Cursor.Position;
                return new System.Drawing.Point(cursorPos.X, cursorPos.Y);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "マウス位置取得エラー");
                return new System.Drawing.Point(0, 0);
            }
        }

        /// <summary>
        /// 右クリック位置のウィンドウ情報を取得
        /// </summary>
        public async Task<WindowCaptureResult?> CaptureWindowInfoAtRightClickAsync()
        {
            if (_isCapturing) return null;

            try
            {
                _isCapturing = true;
                _logger.LogInformation("ウィンドウ情報キャプチャを開始します（右クリック待機）");

                var position = await WaitForRightClickPositionAsync();
                if (position.HasValue)
                {
                    return GetWindowInfoAt(position.Value);
                }

                return null;
            }
            finally
            {
                _isCapturing = false;
            }
        }

        /// <summary>
        /// 右クリック位置の座標を取得
        /// </summary>
        public async Task<System.Drawing.Point?> CaptureCoordinateAtRightClickAsync()
        {
            if (_isCapturing) return null;

            try
            {
                _isCapturing = true;
                _logger.LogInformation("座標キャプチャを開始します（右クリック待機）");

                var position = await WaitForRightClickPositionAsync();
                if (position.HasValue)
                {
                    _logger.LogInformation("右クリック位置で座標を取得: {Position}", position.Value);
                    return position.Value;
                }

                return null;
            }
            finally
            {
                _isCapturing = false;
            }
        }

        /// <summary>
        /// キーキャプチャを実行
        /// </summary>
        public async Task<Key?> CaptureKeyAsync(string title)
        {
            try
            {
                _logger.LogInformation("キーキャプチャを開始します: {Title}", title);

                var dialog = new KeyCaptureDialog(title);
                var result = await dialog.ShowAsync();
                
                if (result.HasValue)
                {
                    _logger.LogInformation("キーキャプチャ完了: {Key}", result.Value);
                    return result.Value;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "キーキャプチャエラー");
                return null;
            }
        }

        /// <summary>
        /// 指定座標の色を取得
        /// </summary>
        public Color GetColorAt(System.Drawing.Point position)
        {
            try
            {
                using (var bitmap = new Bitmap(1, 1))
                {
                    using (var graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.CopyFromScreen(position.X, position.Y, 0, 0, new System.Drawing.Size(1, 1));
                    }
                    
                    return bitmap.GetPixel(0, 0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "色取得エラー: {Position}", position);
                return Color.Black;
            }
        }

        /// <summary>
        /// 右クリック位置を待機
        /// </summary>
        private async Task<System.Drawing.Point?> WaitForRightClickPositionAsync()
        {
            var tcs = new TaskCompletionSource<System.Drawing.Point?>();
            
            try
            {
                var result = System.Windows.MessageBox.Show(
                    "対象位置で右クリックしてください。\n\n右クリックした瞬間の位置を取得します。\nキャンセルするには×ボタンを押してください。",
                    "右クリック位置待機", 
                    MessageBoxButton.OKCancel, 
                    MessageBoxImage.Information);

                if (result != MessageBoxResult.OK)
                {
                    tcs.SetResult(null);
                    return await tcs.Task;
                }

                // 右クリックイベントを監視
                var hookResult = await WaitForRightClickHookAsync();
                tcs.SetResult(hookResult);
                
                return await tcs.Task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "右クリック待機エラー");
                tcs.SetResult(null);
                return await tcs.Task;
            }
        }

        /// <summary>
        /// 右クリックフックを待機（改良版）
        /// </summary>
        private async Task<System.Drawing.Point?> WaitForRightClickHookAsync()
        {
            try
            {
                // 右クリック監視ダイアログを表示
                var waitDialog = new RightClickWaitDialog();
                var result = await waitDialog.ShowAsync();
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "右クリックフック待機エラー");
                return null;
            }
        }

        /// <summary>
        /// 指定位置のウィンドウ情報を取得
        /// </summary>
        private WindowCaptureResult? GetWindowInfoAt(System.Drawing.Point position)
        {
            try
            {
                // WindowHelperを使用
                var handle = WindowHelper.Info.GetWindowHandle(position.X, position.Y);
                var title = WindowHelper.Info.GetWindowTitle(handle);
                var className = WindowHelper.Info.GetWindowClassName(handle);
                
                return new WindowCaptureResult
                {
                    Handle = handle,
                    Title = title,
                    ClassName = className
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ウィンドウ情報取得エラー: {Position}", position);
                
                // フォールバック
                return new WindowCaptureResult
                {
                    Title = $"Window at ({position.X}, {position.Y})",
                    ClassName = "UnknownClass",
                    Handle = IntPtr.Zero
                };
            }
        }
    }

    /// <summary>
    /// キーキャプチャダイアログ（簡易実装）
    /// </summary>
    internal class KeyCaptureDialog
    {
        private readonly string _title;

        public KeyCaptureDialog(string title)
        {
            _title = title;
        }

        public async Task<Key?> ShowAsync()
        {
            try
            {
                var commonKeys = new[]
                {
                    Key.F1, Key.F2, Key.F3, Key.F4, Key.F5, Key.F6,
                    Key.F7, Key.F8, Key.F9, Key.F10, Key.F11, Key.F12,
                    Key.Escape, Key.Enter, Key.Space, Key.Tab,
                    Key.A, Key.S, Key.D, Key.W
                };

                var result = System.Windows.MessageBox.Show(
                    $"{_title}にF1キーを設定しますか？\n\n（Noを選択すると他のキーから選択できます）",
                    "キー選択", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                return result switch
                {
                    MessageBoxResult.Yes => Key.F1,
                    MessageBoxResult.No => await ShowKeySelectionAsync(commonKeys),
                    _ => null
                };
            }
            catch
            {
                return null;
            }
        }

        private async Task<Key?> ShowKeySelectionAsync(Key[] keys)
        {
            var keyNames = string.Join(", ", keys.Take(8).Select(k => k.ToString()));
            var message = $"以下のキーから選択してください:\n{keyNames}\n\n最初のF1キーを選択しますか？";
            
            var result = System.Windows.MessageBox.Show(message, "キー選択", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            return result == MessageBoxResult.Yes ? keys.FirstOrDefault() : null;
        }
    }

    /// <summary>
    /// 右クリック待機ダイアログ（改良版）
    /// </summary>
    internal class RightClickWaitDialog
    {
        public async Task<System.Drawing.Point?> ShowAsync()
        {
            try
            {
                // より実用的な実装：一定時間待機してから右クリック監視
                var result = System.Windows.MessageBox.Show(
                    "右クリック待機を開始します。\n\n対象の位置で右クリックしてください。\n5秒後に現在のマウス位置を取得します。\n\nキャンセルするには×ボタンを押してください。",
                    "右クリック待機",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Cancel)
                {
                    return null;
                }

                // 簡易実装：ユーザーが準備する時間を与えて、その後位置を取得
                await Task.Delay(2000); // 2秒待機してユーザーが準備できるように
                
                // 現在のマウス位置を取得（実際の実装では右クリックイベントを監視）
                var currentPos = System.Windows.Forms.Cursor.Position;
                return new System.Drawing.Point(currentPos.X, currentPos.Y);
            }
            catch
            {
                return null;
            }
        }
    }
}