using System;
using System.Drawing;
using System.Threading.Tasks;

namespace AutoTool.Services.Mouse
{
    /// <summary>
    /// �}�E�X����T�[�r�X�̃C���^�[�t�F�[�X
    /// </summary>
    public interface IMouseService
    {
        /// <summary>
        /// ���݂̃}�E�X�ʒu���擾�i�X�N���[�����W�j
        /// </summary>
        /// <returns>�}�E�X�ʒu</returns>
        Point GetCurrentPosition();

        /// <summary>
        /// �w�肳�ꂽ�E�B���h�E�ł̃N���C�A���g���W���擾
        /// </summary>
        /// <param name="windowTitle">�E�B���h�E�^�C�g��</param>
        /// <param name="windowClassName">�E�B���h�E�N���X���i�I�v�V�����j</param>
        /// <returns>�N���C�A���g���W�A�E�B���h�E��������Ȃ��ꍇ�̓X�N���[�����W</returns>
        Point GetClientPosition(string windowTitle, string? windowClassName = null);

        /// <summary>
        /// �E�N���b�N�ҋ@���[�h���J�n�i�񓯊��j
        /// </summary>
        /// <param name="windowTitle">�ΏۃE�B���h�E�^�C�g���i�I�v�V�����j</param>
        /// <param name="windowClassName">�ΏۃE�B���h�E�N���X���i�I�v�V�����j</param>
        /// <returns>�E�N���b�N���ꂽ���W</returns>
        Task<Point> WaitForRightClickAsync(string? windowTitle = null, string? windowClassName = null);

        /// <summary>
        /// �E�N���b�N�ҋ@���L�����Z��
        /// </summary>
        void CancelRightClickWait();
    }
}