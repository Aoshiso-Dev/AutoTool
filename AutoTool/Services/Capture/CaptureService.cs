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
        public async Task<KeyCaptureResult?> CaptureKeyAsync(string title)
        {
            try
            {
                _logger.LogInformation("キーキャプチャを開始します: {Title}", title);

                var dialog = new KeyCaptureDialog(title);
                var result = await dialog.ShowAsync();
                
                if (result != null)
                {
                    _logger.LogInformation("キーキャプチャ成功: {Key}", result.DisplayText);
                    return result;
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
            try
            {
                var result = System.Windows.MessageBox.Show(
                    "対象位置で右クリックしてください。\n\n右クリックした瞬間の位置を取得します。\nキャンセルするには×ボタンを押してください。",
                    "右クリック位置待機", 
                    MessageBoxButton.OKCancel, 
                    MessageBoxImage.Information);

                if (result != MessageBoxResult.OK)
                {
                    return null;
                }

                // 右クリックイベントを捕捉
                var hookResult = await WaitForRightClickHookAsync();
                return hookResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "右クリック待機エラー");
                return null;
            }
        }

        /// <summary>
        /// 右クリックフックを待機（実装）
        /// </summary>
        private async Task<System.Drawing.Point?> WaitForRightClickHookAsync()
        {
            try
            {
                // 右クリック捕捉ダイアログを表示
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
    /// キーキャプチャダイアログ（WPF実装）
    /// </summary>
    internal class KeyCaptureDialog
    {
        private readonly string _title;

        public KeyCaptureDialog(string title)
        {
            _title = title;
        }

        public async Task<KeyCaptureResult?> ShowAsync()
        {
            return await Task.Run(() =>
            {
                KeyCaptureResult? result = null;
                
                // UIスレッドで実行
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    var window = new KeyCaptureWindow(_title);
                    
                    // オーナーウィンドウを設定（可能であれば）
                    if (System.Windows.Application.Current?.MainWindow != null)
                    {
                        window.Owner = System.Windows.Application.Current.MainWindow;
                    }
                    
                    var dialogResult = window.ShowDialog();
                    
                    if (dialogResult == true && window.CapturedKey.HasValue)
                    {
                        result = new KeyCaptureResult
                        {
                            Key = window.CapturedKey.Value,
                            IsCtrlPressed = window.IsCtrlPressed,
                            IsAltPressed = window.IsAltPressed,
                            IsShiftPressed = window.IsShiftPressed
                        };
                    }
                });
                
                return result;
            });
        }
    }

    /// <summary>
    /// 右クリック待機ダイアログ（実装）
    /// </summary>
    internal class RightClickWaitDialog
    {
        public async Task<System.Drawing.Point?> ShowAsync()
        {
            try
            {
                // 右クリック待機を開始
                var result = System.Windows.MessageBox.Show(
                    "右クリック待機を開始します。\n\n対象の位置で右クリックしてください。\n\nキャンセルするには×ボタンを押してください。",
                    "右クリック待機",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Cancel)
                {
                    return null;
                }

                // MouseHelperのイベントフックを使用して右クリックを待機
                return await WaitForRightClickWithHookAsync();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 実際の右クリックフック処理
        /// </summary>
        private async Task<System.Drawing.Point?> WaitForRightClickWithHookAsync()
        {
            var tcs = new TaskCompletionSource<System.Drawing.Point?>();
            
            try
            {
                // マウスイベントフックを開始
                MouseHelper.Event.StartHook();
                
                // 右クリックイベントハンドラを設定
                void OnRightButtonUp(object? sender, MouseHelper.Event.MouseEventArgs e)
                {
                    tcs.TrySetResult(new System.Drawing.Point(e.X, e.Y));
                }

                MouseHelper.Event.RButtonUp += OnRightButtonUp;

                try
                {
                    // 右クリックを待機（タイムアウト付き）
                    var timeoutTask = Task.Delay(TimeSpan.FromMinutes(1)); // 1分でタイムアウト
                    var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                    
                    if (completedTask == timeoutTask)
                    {
                        tcs.TrySetResult(null); // タイムアウト
                    }
                    
                    return await tcs.Task;
                }
                finally
                {
                    // イベントハンドラを削除
                    MouseHelper.Event.RButtonUp -= OnRightButtonUp;
                }
            }
            finally
            {
                // フックを停止
                MouseHelper.Event.StopHook();
            }
        }
    }}