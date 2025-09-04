using System;
using System.ComponentModel;

namespace AutoTool.ViewModel.Shared
{
    /// <summary>
    /// バックグラウンドクリック方式のアイテム
    /// </summary>
    public class BackgroundClickMethodItem
    {
        public int Value { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 演算子のアイテム
    /// </summary>
    public class OperatorItem
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>
    /// AI検出モードのアイテム
    /// </summary>
    public class AIDetectModeItem
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}