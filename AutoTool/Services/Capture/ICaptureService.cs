using System;
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
        /// <returns>�L���v�`�������L�[�B�L�����Z������null</returns>
        Task<Key?> CaptureKeyAsync(string title);

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
}