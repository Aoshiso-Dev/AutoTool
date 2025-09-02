using System;
using AutoTool.Model.List.Interface;

namespace AutoTool.ViewModel.Helpers
{
    /// <summary>
    /// Phase 5���S�����ŁFEditPanel�v���p�e�B�Ǘ��N���X
    /// MacroPanels�ˑ����폜���A��{�@�\�̂ݎ���
    /// </summary>
    public class EditPanelPropertyManager
    {
        // ��{�v���p�e�B�A�N�Z�T�[
        public PropertyAccessor<ICommandListItem, string> WindowTitle { get; }
        public PropertyAccessor<ICommandListItem, string> WindowClassName { get; }

        public EditPanelPropertyManager()
        {
            // Phase 5: ��{�I�ȃv���p�e�B�A�N�Z�T�[�̂ݎ���
            WindowTitle = new PropertyAccessor<ICommandListItem, string>(
                GetWindowTitle, 
                SetWindowTitle, 
                string.Empty);

            WindowClassName = new PropertyAccessor<ICommandListItem, string>(
                GetWindowClassName, 
                SetWindowClassName, 
                string.Empty);
        }

        private string GetWindowTitle(ICommandListItem? item)
        {
            // Phase 5: BasicCommandItem����WindowTitle���擾
            if (item is AutoTool.Model.List.Type.BasicCommandItem basicItem)
            {
                return basicItem.WindowTitle;
            }
            return string.Empty;
        }

        private void SetWindowTitle(ICommandListItem? item, string value)
        {
            // Phase 5: BasicCommandItem��WindowTitle��ݒ�
            if (item is AutoTool.Model.List.Type.BasicCommandItem basicItem)
            {
                basicItem.WindowTitle = value ?? string.Empty;
            }
        }

        private string GetWindowClassName(ICommandListItem? item)
        {
            // Phase 5: BasicCommandItem����WindowClassName���擾
            if (item is AutoTool.Model.List.Type.BasicCommandItem basicItem)
            {
                return basicItem.WindowClassName;
            }
            return string.Empty;
        }

        private void SetWindowClassName(ICommandListItem? item, string value)
        {
            // Phase 5: BasicCommandItem��WindowClassName��ݒ�
            if (item is AutoTool.Model.List.Type.BasicCommandItem basicItem)
            {
                basicItem.WindowClassName = value ?? string.Empty;
            }
        }
    }
}