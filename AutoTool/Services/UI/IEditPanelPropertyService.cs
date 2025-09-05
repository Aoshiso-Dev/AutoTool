using System.Collections.Generic;
using AutoTool.Model.CommandDefinition;
using AutoTool.Model.List.Interface;

namespace AutoTool.Services.UI
{
    /// <summary>
    /// EditPanel�ݒ�v���p�e�B�T�[�r�X�̃C���^�[�t�F�[�X
    /// </summary>
    public interface IEditPanelPropertyService
    {
        // �A�C�e���^�C�v����v���p�e�B
        bool IsWaitImageItem { get; }
        bool IsClickImageItem { get; }
        bool IsClickImageAIItem { get; }
        bool IsHotkeyItem { get; }
        bool IsClickItem { get; }
        bool IsWaitItem { get; }
        bool IsLoopItem { get; }
        bool IsLoopEndItem { get; }
        bool IsLoopBreakItem { get; }
        bool IsIfImageExistItem { get; }
        bool IsIfImageNotExistItem { get; }
        bool IsIfImageExistAIItem { get; }
        bool IsIfImageNotExistAIItem { get; }
        bool IsIfEndItem { get; }
        bool IsIfVariableItem { get; }
        bool IsExecuteItem { get; }
        bool IsSetVariableItem { get; }
        bool IsSetVariableAIItem { get; }
        bool IsScreenshotItem { get; }

        /// <summary>
        /// �ėp�v���p�e�B�擾
        /// </summary>
        T? GetProperty<T>(string propertyName, T? defaultValue = default);

        /// <summary>
        /// �ėp�v���p�e�B�ݒ�
        /// </summary>
        void SetProperty<T>(string propertyName, T value);

        /// <summary>
        /// �R�}���h�p�̐ݒ��`���擾
        /// </summary>
        List<SettingDefinition> GetSettingDefinitions(string commandType);

        /// <summary>
        /// �R�}���h�A�C�e���̐ݒ�l��K�p
        /// </summary>
        void ApplySettings(ICommandListItem item, Dictionary<string, object?> settings);

        /// <summary>
        /// �R�}���h�A�C�e������ݒ�l���擾
        /// </summary>
        Dictionary<string, object?> GetSettings(ICommandListItem item);

        /// <summary>
        /// �ݒ��`�̃\�[�X�R���N�V�������擾
        /// </summary>
        object[]? GetSourceCollection(string collectionName);
    }
}