using System;
using AutoTool.Model.List.Interface;

namespace AutoTool.ViewModel.Helpers
{
    /// <summary>
    /// EditPanelプロパティ管理クラス
    /// MacroPanels依存を削除し、基本機能のみ実装
    /// </summary>
    public class EditPanelPropertyManager
    {
        // 基本プロパティアクセサー
        public PropertyAccessor<ICommandListItem, string> WindowTitle { get; }
        public PropertyAccessor<ICommandListItem, string> WindowClassName { get; }

        public EditPanelPropertyManager()
        {
            // 基本的なプロパティアクセサーのみ実装
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
            // BasicCommandItemからWindowTitleを取得
            if (item is AutoTool.Model.List.Type.BasicCommandItem basicItem)
            {
                return basicItem.WindowTitle;
            }
            return string.Empty;
        }

        private void SetWindowTitle(ICommandListItem? item, string value)
        {
            // BasicCommandItemにWindowTitleを設定
            if (item is AutoTool.Model.List.Type.BasicCommandItem basicItem)
            {
                basicItem.WindowTitle = value ?? string.Empty;
            }
        }

        private string GetWindowClassName(ICommandListItem? item)
        {
            // BasicCommandItemからWindowClassNameを取得
            if (item is AutoTool.Model.List.Type.BasicCommandItem basicItem)
            {
                return basicItem.WindowClassName;
            }
            return string.Empty;
        }

        private void SetWindowClassName(ICommandListItem? item, string value)
        {
            // BasicCommandItemにWindowClassNameを設定
            if (item is AutoTool.Model.List.Type.BasicCommandItem basicItem)
            {
                basicItem.WindowClassName = value ?? string.Empty;
            }
        }
    }
}