using System;
using AutoTool.Model.List.Interface;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoTool.Model.List.Type
{
    /// <summary>
    /// Phase 5完全版：基本コマンドアイテムクラス（INotifyPropertyChanged対応）
    /// 一時的なスタブで、最終的な具体的なコマンドクラスに置き換える予定
    /// </summary>
    public class BasicCommandItem : ObservableObject, ICommandListItem
    {
        private string _itemType = string.Empty;
        private string _comment = string.Empty;
        private bool _isEnable = true;
        private int _lineNumber;
        private int _nestLevel;
        private string _description = string.Empty;
        private bool _isRunning = false;
        private bool _isSelected = false;
        private bool _isInLoop = false;
        private bool _isInIf = false;
        private int _progress = 0;
        private string _windowTitle = string.Empty;
        private string _windowClassName = string.Empty;

        public string ItemType 
        { 
            get => _itemType; 
            set => SetProperty(ref _itemType, value);
        }

        public string Comment 
        { 
            get => _comment; 
            set => SetProperty(ref _comment, value);
        }

        public bool IsEnable 
        { 
            get => _isEnable; 
            set => SetProperty(ref _isEnable, value);
        }

        public int LineNumber 
        { 
            get => _lineNumber; 
            set => SetProperty(ref _lineNumber, value);
        }

        public int NestLevel 
        { 
            get => _nestLevel; 
            set => SetProperty(ref _nestLevel, value);
        }

        public virtual string Description 
        { 
            get => _description; 
            set => SetProperty(ref _description, value);
        }
        
        // Phase 5: ICommandListItemの必須プロパティを追加
        public bool IsRunning 
        { 
            get => _isRunning; 
            set => SetProperty(ref _isRunning, value);
        }

        public bool IsSelected 
        { 
            get => _isSelected; 
            set => SetProperty(ref _isSelected, value);
        }

        public bool IsInLoop 
        { 
            get => _isInLoop; 
            set => SetProperty(ref _isInLoop, value);
        }

        public bool IsInIf 
        { 
            get => _isInIf; 
            set => SetProperty(ref _isInIf, value);
        }

        public int Progress 
        { 
            get => _progress; 
            set => SetProperty(ref _progress, value);
        }
        
        // Phase 5: 一時的なプロパティ（具体的な実装クラスで正式対応する予定）
        public virtual string WindowTitle 
        { 
            get => _windowTitle; 
            set => SetProperty(ref _windowTitle, value);
        }

        public virtual string WindowClassName 
        { 
            get => _windowClassName; 
            set => SetProperty(ref _windowClassName, value);
        }

        public BasicCommandItem()
        {
            Description = GetDescription();
        }

        public BasicCommandItem(ICommandListItem source)
        {
            if (source != null)
            {
                ItemType = source.ItemType;
                Comment = source.Comment;
                IsEnable = source.IsEnable;
                LineNumber = source.LineNumber;
                NestLevel = source.NestLevel;
                IsRunning = source.IsRunning;
                IsSelected = source.IsSelected;
                IsInLoop = source.IsInLoop;
                Description = source.Description;
            }
        }

        public virtual ICommandListItem Clone()
        {
            return new BasicCommandItem
            {
                ItemType = this.ItemType,
                Comment = this.Comment,
                IsEnable = this.IsEnable,
                LineNumber = this.LineNumber,
                NestLevel = this.NestLevel,
                IsRunning = this.IsRunning,
                IsSelected = this.IsSelected,
                IsInLoop = this.IsInLoop,
                Description = this.Description,
                WindowTitle = this.WindowTitle,
                WindowClassName = this.WindowClassName
            };
        }

        protected virtual string GetDescription()
        {
            return ItemType switch
            {
                "Click" => "マウスクリック",
                "Click_Image" => "画像をクリック",
                "Click_Image_AI" => "AI画像をクリック",
                "Wait" => "待機",
                "Wait_Image" => "画像を待機",
                "Hotkey" => "ホットキー送信",
                "Loop" => "ループ開始",
                "Loop_End" => "ループ終了",
                "Loop_Break" => "ループ脱出",
                "IF_ImageExist" => "画像存在判定",
                "IF_ImageNotExist" => "画像非存在判定",
                "IF_ImageExist_AI" => "AI画像存在判定",
                "IF_ImageNotExist_AI" => "AI画像非存在判定",
                "IF_End" => "条件分岐終了",
                "Execute" => "プログラム実行",
                "SetVariable" => "変数設定",
                "SetVariable_AI" => "AI変数設定",
                "IF_Variable" => "変数条件分岐",
                "Screenshot" => "スクリーンショット",
                _ => ItemType
            };
        }

        public override string ToString()
        {
            return $"[{LineNumber}] {ItemType}: {Comment}";
        }
    }

    /// <summary>
    /// Phase 5完全版：If系コマンド用インターフェース
    /// </summary>
    public interface IIfItem : ICommandListItem
    {
        ICommandListItem? Pair { get; set; }
    }

    /// <summary>
    /// Phase 5完全版：IfEnd用インターフェース
    /// </summary>
    public interface IIfEndItem : ICommandListItem
    {
        ICommandListItem? Pair { get; set; }
    }

    /// <summary>
    /// Phase 5完全版：Loop系コマンド用インターフェース
    /// </summary>
    public interface ILoopItem : ICommandListItem
    {
        ICommandListItem? Pair { get; set; }
    }

    /// <summary>
    /// Phase 5完全版：LoopEnd用インターフェース
    /// </summary>
    public interface ILoopEndItem : ICommandListItem
    {
        ICommandListItem? Pair { get; set; }
    }

    /// <summary>
    /// Phase 5完全版：拡張BasicCommandItem（ペアリング対応）
    /// </summary>
    public class PairableCommandItem : BasicCommandItem, IIfItem, IIfEndItem, ILoopItem, ILoopEndItem
    {
        private ICommandListItem? _pair;

        public ICommandListItem? Pair 
        { 
            get => _pair; 
            set => SetProperty(ref _pair, value);
        }

        public PairableCommandItem() : base() { }

        public PairableCommandItem(ICommandListItem source) : base(source) { }

        public override ICommandListItem Clone()
        {
            return new PairableCommandItem
            {
                ItemType = this.ItemType,
                Comment = this.Comment,
                IsEnable = this.IsEnable,
                LineNumber = this.LineNumber,
                NestLevel = this.NestLevel,
                IsRunning = this.IsRunning,
                IsSelected = this.IsSelected,
                IsInLoop = this.IsInLoop,
                Description = this.Description,
                WindowTitle = this.WindowTitle,
                WindowClassName = this.WindowClassName,
                Pair = null // ペアリングは後で再設定される
            };
        }
    }
}