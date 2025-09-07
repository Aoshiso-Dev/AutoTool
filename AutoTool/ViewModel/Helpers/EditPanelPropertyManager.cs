using System;
using AutoTool.ViewModel.Shared;

namespace AutoTool.ViewModel.Helpers
{
    /*
    /// <summary>
    /// EditPanelプロパティ管理クラス
    /// UniversalCommandItem 専用版
    /// </summary>
    public class EditPanelPropertyManager
    {
        // 基本プロパティアクセサー
        public PropertyAccessor<UniversalCommandItem, string> WindowTitle { get; }
        public PropertyAccessor<UniversalCommandItem, string> WindowClassName { get; }

        public EditPanelPropertyManager()
        {
            // 基本的なプロパティアクセサーのみ実装
            WindowTitle = new PropertyAccessor<UniversalCommandItem, string>(
                GetWindowTitle, 
                SetWindowTitle, 
                string.Empty);

            WindowClassName = new PropertyAccessor<UniversalCommandItem, string>(
                GetWindowClassName, 
                SetWindowClassName, 
                string.Empty);
        }

        private string GetWindowTitle(UniversalCommandItem? item)
        {
            return item?.GetSetting<string>("WindowTitle") ?? string.Empty;
        }

        private void SetWindowTitle(UniversalCommandItem? item, string value)
        {
            item?.SetSetting("WindowTitle", value ?? string.Empty);
        }

        private string GetWindowClassName(UniversalCommandItem? item)
        {
            return item?.GetSetting<string>("WindowClassName") ?? string.Empty;
        }

        private void SetWindowClassName(UniversalCommandItem? item, string value)
        {
            item?.SetSetting("WindowClassName", value ?? string.Empty);
        }
    }
    */
}