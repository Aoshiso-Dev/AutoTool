using System;
using AutoTool.Model.List.Interface;

namespace AutoTool.Model.List.Type
{
    /// <summary>
    /// Phase 5統合版：基本コマンドアイテムクラス
    /// 一時的なスタブ実装、後で具体的なコマンドクラスに置き換え予定
    /// </summary>
    public class BasicCommandItem : ICommandListItem
    {
        public string ItemType { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public bool IsEnable { get; set; } = true;
        public int LineNumber { get; set; }
        public int NestLevel { get; set; }
        public virtual string Description { get; set; } = string.Empty;
        
        // Phase 5: ICommandListItemの不足プロパティを追加
        public bool IsRunning { get; set; } = false;
        public bool IsSelected { get; set; } = false;
        public bool IsInLoop { get; set; } = false;
        public bool IsInIf { get; set; } = false;
        public int Progress { get; set; } = 0;
        
        // Phase 5: 一時的なプロパティ（具体的な実装クラスで正しく実装予定）
        public virtual string WindowTitle { get; set; } = string.Empty;
        public virtual string WindowClassName { get; set; } = string.Empty;

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
                "IF_Variable" => "変数条件判定",
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
    /// Phase 5統合版：If系コマンド用インターフェース
    /// </summary>
    public interface IIfItem : ICommandListItem
    {
        ICommandListItem? Pair { get; set; }
    }

    /// <summary>
    /// Phase 5統合版：IfEnd用インターフェース
    /// </summary>
    public interface IIfEndItem : ICommandListItem
    {
        ICommandListItem? Pair { get; set; }
    }

    /// <summary>
    /// Phase 5統合版：Loop系コマンド用インターフェース
    /// </summary>
    public interface ILoopItem : ICommandListItem
    {
        ICommandListItem? Pair { get; set; }
    }

    /// <summary>
    /// Phase 5統合版：LoopEnd用インターフェース
    /// </summary>
    public interface ILoopEndItem : ICommandListItem
    {
        ICommandListItem? Pair { get; set; }
    }

    /// <summary>
    /// Phase 5統合版：拡張BasicCommandItem（ペアリング対応）
    /// </summary>
    public class PairableCommandItem : BasicCommandItem, IIfItem, IIfEndItem, ILoopItem, ILoopEndItem
    {
        public ICommandListItem? Pair { get; set; }

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