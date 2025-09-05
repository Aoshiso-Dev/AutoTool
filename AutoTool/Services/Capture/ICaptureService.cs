using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AutoTool.Services.Capture
{
    /// <summary>
    /// �L���v�`���T�[�r�X�̃C���^�[�t�F�[�X
    /// </summary>
    public interface ICaptureService
    {
        /// <summary>
        /// �E�N���b�N�ʒu�̐F���擾
        /// </summary>
        /// <returns>�擾�����F�B�L�����Z������null</returns>
        Task<Color?> CaptureColorAtRightClickAsync();

        /// <summary>
        /// ���݂̃}�E�X�ʒu���擾
        /// </summary>
        /// <returns>�}�E�X�ʒu</returns>
        System.Drawing.Point GetCurrentMousePosition();

        /// <summary>
        /// �E�N���b�N�ʒu�̃E�B���h�E�����擾
        /// </summary>
        /// <returns>�E�B���h�E���B�L�����Z������null</returns>
        Task<WindowCaptureResult?> CaptureWindowInfoAtRightClickAsync();

        /// <summary>
        /// �E�N���b�N�ʒu�̍��W���擾
        /// </summary>
        /// <returns>�擾�������W�B�L�����Z������null</returns>
        Task<System.Drawing.Point?> CaptureCoordinateAtRightClickAsync();

        /// <summary>
        /// �L�[�L���v�`�������s
        /// </summary>
        /// <param name="title">�_�C�A���O�^�C�g��</param>
        /// <returns>�L���v�`�������L�[���B�L�����Z������null</returns>
        Task<KeyCaptureResult?> CaptureKeyAsync(string title);

        /// <summary>
        /// �w����W�̐F���擾
        /// </summary>
        /// <param name="position">���W</param>
        /// <returns>�F</returns>
        Color GetColorAt(System.Drawing.Point position);
    }

    /// <summary>
    /// �E�B���h�E�L���v�`������
    /// </summary>
    public class WindowCaptureResult
    {
        public string Title { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public IntPtr Handle { get; set; } = IntPtr.Zero;

        public override string ToString()
        {
            return string.IsNullOrEmpty(ClassName) ? Title : $"{Title} ({ClassName})";
        }
    }

    /// <summary>
    /// �L�[�L���v�`������
    /// </summary>
    public class KeyCaptureResult
    {
        public Key Key { get; set; }
        public bool IsCtrlPressed { get; set; }
        public bool IsAltPressed { get; set; }
        public bool IsShiftPressed { get; set; }

        /// <summary>
        /// �L�[�̕\���p��������擾
        /// </summary>
        public string DisplayText
        {
            get
            {
                var parts = new List<string>();

                if (IsCtrlPressed) parts.Add("Ctrl");
                if (IsAltPressed) parts.Add("Alt");
                if (IsShiftPressed) parts.Add("Shift");
                
                parts.Add(GetKeyDisplayName(Key));

                return string.Join(" + ", parts);
            }
        }

        private static string GetKeyDisplayName(Key key)
        {
            return key switch
            {
                // �t�@���N�V�����L�[
                Key.F1 => "F1", Key.F2 => "F2", Key.F3 => "F3", Key.F4 => "F4",
                Key.F5 => "F5", Key.F6 => "F6", Key.F7 => "F7", Key.F8 => "F8",
                Key.F9 => "F9", Key.F10 => "F10", Key.F11 => "F11", Key.F12 => "F12",

                // �����L�[
                Key.D0 => "0", Key.D1 => "1", Key.D2 => "2", Key.D3 => "3", Key.D4 => "4",
                Key.D5 => "5", Key.D6 => "6", Key.D7 => "7", Key.D8 => "8", Key.D9 => "9",

                // ����L�[
                Key.Space => "Space", Key.Enter => "Enter", Key.Escape => "Escape",
                Key.Tab => "Tab", Key.Back => "Backspace", Key.Delete => "Delete",

                // ���L�[
                Key.Up => "��", Key.Down => "��", Key.Left => "��", Key.Right => "��",

                // ���̑�
                _ => key.ToString()
            };
        }

        public override string ToString() => DisplayText;
    }
}