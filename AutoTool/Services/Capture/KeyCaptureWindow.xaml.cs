using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace AutoTool.Services.Capture
{
    /// <summary>
    /// �L�[�L���v�`���E�B���h�E
    /// </summary>
    public partial class KeyCaptureWindow : System.Windows.Window, INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private string _capturedKeyText = string.Empty;
        private Key? _capturedKey = null;
        private bool _isCtrlPressed = false;
        private bool _isAltPressed = false;
        private bool _isShiftPressed = false;
        private readonly HashSet<Key> _pressedKeys = new();

        public KeyCaptureWindow(string title)
        {
            InitializeComponent();
            DataContext = this;
            Title = "Key Capture";
            TitleText = $"{title} - Set Key";
        }

        #region Properties

        /// <summary>
        /// �^�C�g���\���p�e�L�X�g
        /// </summary>
        public string TitleText
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// �L���v�`�����ꂽ�L�[�̃e�L�X�g�\��
        /// </summary>
        public string CapturedKeyText
        {
            get => _capturedKeyText;
            set
            {
                _capturedKeyText = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// �L���v�`�����ꂽ�L�[
        /// </summary>
        public Key? CapturedKey => _capturedKey;

        /// <summary>
        /// Ctrl�L�[��������Ă��邩
        /// </summary>
        public bool IsCtrlPressed => _isCtrlPressed;

        /// <summary>
        /// Alt�L�[��������Ă��邩
        /// </summary>
        public bool IsAltPressed => _isAltPressed;

        /// <summary>
        /// Shift�L�[��������Ă��邩
        /// </summary>
        public bool IsShiftPressed => _isShiftPressed;

        #endregion

        #region Event Handlers

        private void KeyTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true; // �ʏ�̃L�[�����𖳌���

            var key = e.Key;
            
            // �V�X�e���L�[�̏ꍇ�́ASystemKey���g�p
            if (key == Key.System)
            {
                key = e.SystemKey;
            }

            // �C���L�[�̏�Ԃ��X�V
            _isCtrlPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            _isAltPressed = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
            _isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            // �C���L�[�݂̂̏ꍇ�͉������Ȃ�
            if (IsModifierKey(key))
            {
                UpdateKeyText();
                return;
            }

            // �L���ȃL�[�̏ꍇ�A�L���v�`�����s
            if (IsValidKey(key))
            {
                _capturedKey = key;
                _pressedKeys.Add(key);
                UpdateKeyText();
            }
        }

        private void KeyTextBox_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;

            // �C���L�[�̏�Ԃ��X�V
            _isCtrlPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            _isAltPressed = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
            _isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            UpdateKeyText();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (_capturedKey.HasValue)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                System.Windows.MessageBox.Show("No key has been set.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearCapture();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            // �E�B���h�E���A�N�e�B�u�ɂȂ����Ƃ��Ƀe�L�X�g�{�b�N�X�Ƀt�H�[�J�X
            KeyTextBox.Focus();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// �C���L�[���ǂ������`�F�b�N
        /// </summary>
        private static bool IsModifierKey(Key key)
        {
            return key == Key.LeftCtrl || key == Key.RightCtrl ||
                   key == Key.LeftAlt || key == Key.RightAlt ||
                   key == Key.LeftShift || key == Key.RightShift ||
                   key == Key.LWin || key == Key.RWin;
        }

        /// <summary>
        /// �L���ȃL�[���ǂ������`�F�b�N
        /// </summary>
        private static bool IsValidKey(Key key)
        {
            // �C���L�[�͏��O
            if (IsModifierKey(key))
                return false;

            // ����L�[�͏��O
            if (key == Key.None || key == Key.DeadCharProcessed)
                return false;

            return true;
        }

        /// <summary>
        /// �L�[�e�L�X�g���X�V
        /// </summary>
        private void UpdateKeyText()
        {
            var parts = new List<string>();

            if (_isCtrlPressed)
                parts.Add("Ctrl");
            if (_isAltPressed)
                parts.Add("Alt");
            if (_isShiftPressed)
                parts.Add("Shift");

            if (_capturedKey.HasValue)
            {
                parts.Add(GetKeyDisplayName(_capturedKey.Value));
            }

            CapturedKeyText = parts.Count > 0 ? string.Join(" + ", parts) : "";
        }

        /// <summary>
        /// �L�[�̕\�������擾
        /// </summary>
        private static string GetKeyDisplayName(Key key)
        {
            return key switch
            {
                // �t�@���N�V�����L�[
                Key.F1 => "F1",
                Key.F2 => "F2",
                Key.F3 => "F3",
                Key.F4 => "F4",
                Key.F5 => "F5",
                Key.F6 => "F6",
                Key.F7 => "F7",
                Key.F8 => "F8",
                Key.F9 => "F9",
                Key.F10 => "F10",
                Key.F11 => "F11",
                Key.F12 => "F12",

                // �����L�[
                Key.D0 => "0",
                Key.D1 => "1",
                Key.D2 => "2",
                Key.D3 => "3",
                Key.D4 => "4",
                Key.D5 => "5",
                Key.D6 => "6",
                Key.D7 => "7",
                Key.D8 => "8",
                Key.D9 => "9",

                // ����L�[
                Key.Space => "Space",
                Key.Enter => "Enter",
                Key.Escape => "Escape",
                Key.Tab => "Tab",
                Key.Back => "Backspace",
                Key.Delete => "Delete",
                Key.Insert => "Insert",
                Key.Home => "Home",
                Key.End => "End",
                Key.PageUp => "PageUp",
                Key.PageDown => "PageDown",

                // ���L�[
                Key.Up => "��",
                Key.Down => "��",
                Key.Left => "��",
                Key.Right => "��",

                // �e���L�[
                Key.NumPad0 => "Num0",
                Key.NumPad1 => "Num1",
                Key.NumPad2 => "Num2",
                Key.NumPad3 => "Num3",
                Key.NumPad4 => "Num4",
                Key.NumPad5 => "Num5",
                Key.NumPad6 => "Num6",
                Key.NumPad7 => "Num7",
                Key.NumPad8 => "Num8",
                Key.NumPad9 => "Num9",

                // ���̑�
                _ => key.ToString()
            };
        }

        /// <summary>
        /// �L���v�`�����N���A
        /// </summary>
        private void ClearCapture()
        {
            _capturedKey = null;
            _pressedKeys.Clear();
            CapturedKeyText = "";
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}