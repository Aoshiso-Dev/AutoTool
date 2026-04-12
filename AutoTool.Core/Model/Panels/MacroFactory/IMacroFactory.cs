using AutoTool.Commands.Interface;
using AutoTool.Panels.Model.List.Interface;

namespace AutoTool.Panels.Model.MacroFactory;

/// <summary>
/// �}�N���t�@�N�g���̃C���^�[�t�F�[�X
/// </summary>
public interface IMacroFactory
{
    /// <summary>
    /// �R�}���h���X�g�A�C�e������}�N����쐬���܂�
    /// </summary>
    /// <param name="items">�R�}���h���X�g�A�C�e��</param>
    /// <returns>���s�\�ȃ}�N���R�}���h</returns>
    ICommand CreateMacro(IEnumerable<ICommandListItem> items);
}

