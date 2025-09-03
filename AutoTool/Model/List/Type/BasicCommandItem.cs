using System;
using AutoTool.Model.List.Interface;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoTool.Model.List.Type
{
    /// <summary>
    /// Phase 5���S�ŁF��{�R�}���h�A�C�e���N���X�iINotifyPropertyChanged�Ή��j
    /// �ꎞ�I�ȃX�^�u�ŁA�ŏI�I�ȋ�̓I�ȃR�}���h�N���X�ɒu��������\��
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
        
        // Phase 5: ICommandListItem�̕K�{�v���p�e�B��ǉ�
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
        
        // Phase 5: �ꎞ�I�ȃv���p�e�B�i��̓I�Ȏ����N���X�Ő����Ή�����\��j
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
    /// Phase 5���S�ŁFIf�n�R�}���h�p�C���^�[�t�F�[�X
    /// </summary>
    public interface IIfItem : ICommandListItem
    {
        ICommandListItem? Pair { get; set; }
    }

    /// <summary>
    /// Phase 5���S�ŁFIfEnd�p�C���^�[�t�F�[�X
    /// </summary>
    public interface IIfEndItem : ICommandListItem
    {
        ICommandListItem? Pair { get; set; }
    }

    /// <summary>
    /// Phase 5���S�ŁFLoop�n�R�}���h�p�C���^�[�t�F�[�X
    /// </summary>
    public interface ILoopItem : ICommandListItem
    {
        ICommandListItem? Pair { get; set; }
    }

    /// <summary>
    /// Phase 5���S�ŁFLoopEnd�p�C���^�[�t�F�[�X
    /// </summary>
    public interface ILoopEndItem : ICommandListItem
    {
        ICommandListItem? Pair { get; set; }
    }

    /// <summary>
    /// Phase 5���S�ŁF�g��BasicCommandItem�i�y�A�����O�Ή��j
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
                Pair = null // �y�A�����O�͌�ōĐݒ肳���
            };
        }
    }
}