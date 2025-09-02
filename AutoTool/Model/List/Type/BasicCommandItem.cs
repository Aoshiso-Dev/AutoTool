using System;
using AutoTool.Model.List.Interface;

namespace AutoTool.Model.List.Type
{
    /// <summary>
    /// Phase 5�����ŁF��{�R�}���h�A�C�e���N���X
    /// �ꎞ�I�ȃX�^�u�����A��ŋ�̓I�ȃR�}���h�N���X�ɒu�������\��
    /// </summary>
    public class BasicCommandItem : ICommandListItem
    {
        public string ItemType { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public bool IsEnable { get; set; } = true;
        public int LineNumber { get; set; }
        public int NestLevel { get; set; }
        public virtual string Description { get; set; } = string.Empty;
        
        // Phase 5: ICommandListItem�̕s���v���p�e�B��ǉ�
        public bool IsRunning { get; set; } = false;
        public bool IsSelected { get; set; } = false;
        public bool IsInLoop { get; set; } = false;
        public bool IsInIf { get; set; } = false;
        public int Progress { get; set; } = 0;
        
        // Phase 5: �ꎞ�I�ȃv���p�e�B�i��̓I�Ȏ����N���X�Ő����������\��j
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
                "Click" => "�}�E�X�N���b�N",
                "Click_Image" => "�摜���N���b�N",
                "Click_Image_AI" => "AI�摜���N���b�N",
                "Wait" => "�ҋ@",
                "Wait_Image" => "�摜��ҋ@",
                "Hotkey" => "�z�b�g�L�[���M",
                "Loop" => "���[�v�J�n",
                "Loop_End" => "���[�v�I��",
                "Loop_Break" => "���[�v�E�o",
                "IF_ImageExist" => "�摜���ݔ���",
                "IF_ImageNotExist" => "�摜�񑶍ݔ���",
                "IF_ImageExist_AI" => "AI�摜���ݔ���",
                "IF_ImageNotExist_AI" => "AI�摜�񑶍ݔ���",
                "IF_End" => "��������I��",
                "Execute" => "�v���O�������s",
                "SetVariable" => "�ϐ��ݒ�",
                "SetVariable_AI" => "AI�ϐ��ݒ�",
                "IF_Variable" => "�ϐ���������",
                "Screenshot" => "�X�N���[���V���b�g",
                _ => ItemType
            };
        }

        public override string ToString()
        {
            return $"[{LineNumber}] {ItemType}: {Comment}";
        }
    }

    /// <summary>
    /// Phase 5�����ŁFIf�n�R�}���h�p�C���^�[�t�F�[�X
    /// </summary>
    public interface IIfItem : ICommandListItem
    {
        ICommandListItem? Pair { get; set; }
    }

    /// <summary>
    /// Phase 5�����ŁFIfEnd�p�C���^�[�t�F�[�X
    /// </summary>
    public interface IIfEndItem : ICommandListItem
    {
        ICommandListItem? Pair { get; set; }
    }

    /// <summary>
    /// Phase 5�����ŁFLoop�n�R�}���h�p�C���^�[�t�F�[�X
    /// </summary>
    public interface ILoopItem : ICommandListItem
    {
        ICommandListItem? Pair { get; set; }
    }

    /// <summary>
    /// Phase 5�����ŁFLoopEnd�p�C���^�[�t�F�[�X
    /// </summary>
    public interface ILoopEndItem : ICommandListItem
    {
        ICommandListItem? Pair { get; set; }
    }

    /// <summary>
    /// Phase 5�����ŁF�g��BasicCommandItem�i�y�A�����O�Ή��j
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
                Pair = null // �y�A�����O�͌�ōĐݒ肳���
            };
        }
    }
}