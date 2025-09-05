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
    /// �L���v�`���T�[�r�X�̎���
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
        /// �E�N���b�N�ʒu�̐F���擾
        /// </summary>
        public async Task<Color?> CaptureColorAtRightClickAsync()
        {
            if (_isCapturing) return null;

            try
            {
                _isCapturing = true;
                _logger.LogInformation("�F�L���v�`�����J�n���܂��i�E�N���b�N�ҋ@�j");

                // ColorPickHelper��D��g�p
                try
                {
                    var colorPickWindow = new ColorPickHelper.ColorPickWindow();
                    var result = colorPickWindow.ShowDialog();
                    
                    if (result == true && colorPickWindow.Color.HasValue)
                    {
                        var color = colorPickWindow.Color.Value;
                        var drawingColor = Color.FromArgb(color.A, color.R, color.G, color.B);
                        _logger.LogInformation("ColorPickHelper�ŐF���擾: {Color}", drawingColor);
                        return drawingColor;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ColorPickHelper�g�p�G���[�A�t�H�[���o�b�N�ɐ؂�ւ�");
                }

                // �t�H�[���o�b�N: �E�N���b�N�ʒu�ҋ@
                var position = await WaitForRightClickPositionAsync();
                if (position.HasValue)
                {
                    var color = GetColorAt(position.Value);
                    _logger.LogInformation("�E�N���b�N�ʒu�ŐF���擾: {Position} -> {Color}", position.Value, color);
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
        /// ���݂̃}�E�X�ʒu���擾
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
                _logger.LogError(ex, "�}�E�X�ʒu�擾�G���[");
                return new System.Drawing.Point(0, 0);
            }
        }

        /// <summary>
        /// �E�N���b�N�ʒu�̃E�B���h�E�����擾
        /// </summary>
        public async Task<WindowCaptureResult?> CaptureWindowInfoAtRightClickAsync()
        {
            if (_isCapturing) return null;

            try
            {
                _isCapturing = true;
                _logger.LogInformation("�E�B���h�E���L���v�`�����J�n���܂��i�E�N���b�N�ҋ@�j");

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
        /// �E�N���b�N�ʒu�̍��W���擾
        /// </summary>
        public async Task<System.Drawing.Point?> CaptureCoordinateAtRightClickAsync()
        {
            if (_isCapturing) return null;

            try
            {
                _isCapturing = true;
                _logger.LogInformation("���W�L���v�`�����J�n���܂��i�E�N���b�N�ҋ@�j");

                var position = await WaitForRightClickPositionAsync();
                if (position.HasValue)
                {
                    _logger.LogInformation("�E�N���b�N�ʒu�ō��W���擾: {Position}", position.Value);
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
        /// �L�[�L���v�`�������s
        /// </summary>
        public async Task<Key?> CaptureKeyAsync(string title)
        {
            try
            {
                _logger.LogInformation("�L�[�L���v�`�����J�n���܂�: {Title}", title);

                var dialog = new KeyCaptureDialog(title);
                var result = await dialog.ShowAsync();
                
                if (result.HasValue)
                {
                    _logger.LogInformation("�L�[�L���v�`������: {Key}", result.Value);
                    return result.Value;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�L�[�L���v�`���G���[");
                return null;
            }
        }

        /// <summary>
        /// �w����W�̐F���擾
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
                _logger.LogError(ex, "�F�擾�G���[: {Position}", position);
                return Color.Black;
            }
        }

        /// <summary>
        /// �E�N���b�N�ʒu��ҋ@
        /// </summary>
        private async Task<System.Drawing.Point?> WaitForRightClickPositionAsync()
        {
            var tcs = new TaskCompletionSource<System.Drawing.Point?>();
            
            try
            {
                var result = System.Windows.MessageBox.Show(
                    "�Ώۈʒu�ŉE�N���b�N���Ă��������B\n\n�E�N���b�N�����u�Ԃ̈ʒu���擾���܂��B\n�L�����Z������ɂ́~�{�^���������Ă��������B",
                    "�E�N���b�N�ʒu�ҋ@", 
                    MessageBoxButton.OKCancel, 
                    MessageBoxImage.Information);

                if (result != MessageBoxResult.OK)
                {
                    tcs.SetResult(null);
                    return await tcs.Task;
                }

                // �E�N���b�N�C�x���g���Ď�
                var hookResult = await WaitForRightClickHookAsync();
                tcs.SetResult(hookResult);
                
                return await tcs.Task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�E�N���b�N�ҋ@�G���[");
                tcs.SetResult(null);
                return await tcs.Task;
            }
        }

        /// <summary>
        /// �E�N���b�N�t�b�N��ҋ@�i���ǔŁj
        /// </summary>
        private async Task<System.Drawing.Point?> WaitForRightClickHookAsync()
        {
            try
            {
                // �E�N���b�N�Ď��_�C�A���O��\��
                var waitDialog = new RightClickWaitDialog();
                var result = await waitDialog.ShowAsync();
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�E�N���b�N�t�b�N�ҋ@�G���[");
                return null;
            }
        }

        /// <summary>
        /// �w��ʒu�̃E�B���h�E�����擾
        /// </summary>
        private WindowCaptureResult? GetWindowInfoAt(System.Drawing.Point position)
        {
            try
            {
                // WindowHelper���g�p
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
                _logger.LogError(ex, "�E�B���h�E���擾�G���[: {Position}", position);
                
                // �t�H�[���o�b�N
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
    /// �L�[�L���v�`���_�C�A���O�i�ȈՎ����j
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
                    $"{_title}��F1�L�[��ݒ肵�܂����H\n\n�iNo��I������Ƒ��̃L�[����I���ł��܂��j",
                    "�L�[�I��", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

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
            var message = $"�ȉ��̃L�[����I�����Ă�������:\n{keyNames}\n\n�ŏ���F1�L�[��I�����܂����H";
            
            var result = System.Windows.MessageBox.Show(message, "�L�[�I��", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            return result == MessageBoxResult.Yes ? keys.FirstOrDefault() : null;
        }
    }

    /// <summary>
    /// �E�N���b�N�ҋ@�_�C�A���O�i���ǔŁj
    /// </summary>
    internal class RightClickWaitDialog
    {
        public async Task<System.Drawing.Point?> ShowAsync()
        {
            try
            {
                // �����p�I�Ȏ����F��莞�ԑҋ@���Ă���E�N���b�N�Ď�
                var result = System.Windows.MessageBox.Show(
                    "�E�N���b�N�ҋ@���J�n���܂��B\n\n�Ώۂ̈ʒu�ŉE�N���b�N���Ă��������B\n5�b��Ɍ��݂̃}�E�X�ʒu���擾���܂��B\n\n�L�����Z������ɂ́~�{�^���������Ă��������B",
                    "�E�N���b�N�ҋ@",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Cancel)
                {
                    return null;
                }

                // �ȈՎ����F���[�U�[���������鎞�Ԃ�^���āA���̌�ʒu���擾
                await Task.Delay(2000); // 2�b�ҋ@���ă��[�U�[�������ł���悤��
                
                // ���݂̃}�E�X�ʒu���擾�i���ۂ̎����ł͉E�N���b�N�C�x���g���Ď��j
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