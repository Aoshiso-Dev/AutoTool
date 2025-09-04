using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using AutoTool.Model.List.Interface;
using AutoTool.Message;
using AutoTool.Services.UI;

namespace AutoTool.Services.UI
{
    /// <summary>
    /// MainWindowの編集パネル統合機能を管理するサービス
    /// </summary>
    public interface IEditPanelIntegrationService
    {
        ICommandListItem? SelectedItem { get; set; }
        
        // 状態プロパティ
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
        
        // 設定プロパティ
        string Comment { get; set; }
        string WindowTitle { get; set; }
        string WindowClassName { get; set; }
        string ImagePath { get; set; }
        double Threshold { get; set; }
        int Timeout { get; set; }
        int Interval { get; set; }
        
        // イベント
        event EventHandler<ICommandListItem?> SelectedItemChanged;
        
        // メソッド
        void UpdateFromEditPanel();
    }

    /// <summary>
    /// MainWindowの編集パネル統合機能サービス実装（EditPanelPropertyService使用版）
    /// </summary>
    [Obsolete("EditPanelPropertyServiceを直接使用してください。この統合サービスは廃止予定です。", false)]
    public class EditPanelIntegrationService : ObservableObject, IEditPanelIntegrationService
    {
        private readonly ILogger<EditPanelIntegrationService> _logger;
        private readonly IEditPanelPropertyService _editPanelPropertyService;
        private readonly IMessenger _messenger;

        private ICommandListItem? _selectedItem;

        public ICommandListItem? SelectedItem 
        { 
            get => _selectedItem; 
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    NotifySelectedItemChanged(value);
                }
            }
        }

        public event EventHandler<ICommandListItem?>? SelectedItemChanged;

        public EditPanelIntegrationService(
            ILogger<EditPanelIntegrationService> logger,
            IEditPanelPropertyService editPanelPropertyService,
            IMessenger messenger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _editPanelPropertyService = editPanelPropertyService ?? throw new ArgumentNullException(nameof(editPanelPropertyService));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            SetupMessaging();
        }

        private void SetupMessaging()
        {
            _messenger.Register<ChangeSelectedMessage>(this, (r, m) =>
            {
                SelectedItem = m.SelectedItem;
            });
        }

        private void NotifySelectedItemChanged(ICommandListItem? value)
        {
            // EditPanelPropertyServiceにアイテム変更を通知
            _messenger.Send(new UpdateEditPanelItemMessage(value));
            SelectedItemChanged?.Invoke(this, value);
        }

        public void UpdateFromEditPanel()
        {
            OnPropertyChanged(nameof(IsWaitImageItem));
            OnPropertyChanged(nameof(IsClickImageItem));
            OnPropertyChanged(nameof(IsClickImageAIItem));
            OnPropertyChanged(nameof(IsHotkeyItem));
            OnPropertyChanged(nameof(IsClickItem));
            OnPropertyChanged(nameof(IsWaitItem));
            OnPropertyChanged(nameof(IsLoopItem));
            OnPropertyChanged(nameof(IsLoopEndItem));
            OnPropertyChanged(nameof(IsLoopBreakItem));
            OnPropertyChanged(nameof(IsIfImageExistItem));
            OnPropertyChanged(nameof(IsIfImageNotExistItem));
            OnPropertyChanged(nameof(IsIfImageExistAIItem));
            OnPropertyChanged(nameof(IsIfImageNotExistAIItem));
            OnPropertyChanged(nameof(IsIfEndItem));
            OnPropertyChanged(nameof(IsIfVariableItem));
            OnPropertyChanged(nameof(IsExecuteItem));
            OnPropertyChanged(nameof(IsSetVariableItem));
            OnPropertyChanged(nameof(IsSetVariableAIItem));
            OnPropertyChanged(nameof(IsScreenshotItem));
        }

        // EditPanelPropertyServiceへのプロキシプロパティ
        public bool IsWaitImageItem => _editPanelPropertyService?.IsWaitImageItem ?? false;
        public bool IsClickImageItem => _editPanelPropertyService?.IsClickImageItem ?? false;
        public bool IsClickImageAIItem => _editPanelPropertyService?.IsClickImageAIItem ?? false;
        public bool IsHotkeyItem => _editPanelPropertyService?.IsHotkeyItem ?? false;
        public bool IsClickItem => _editPanelPropertyService?.IsClickItem ?? false;
        public bool IsWaitItem => _editPanelPropertyService?.IsWaitItem ?? false;
        public bool IsLoopItem => _editPanelPropertyService?.IsLoopItem ?? false;
        public bool IsLoopEndItem => _editPanelPropertyService?.IsLoopEndItem ?? false;
        public bool IsLoopBreakItem => _editPanelPropertyService?.IsLoopBreakItem ?? false;
        public bool IsIfImageExistItem => _editPanelPropertyService?.IsIfImageExistItem ?? false;
        public bool IsIfImageNotExistItem => _editPanelPropertyService?.IsIfImageNotExistItem ?? false;
        public bool IsIfImageExistAIItem => _editPanelPropertyService?.IsIfImageExistAIItem ?? false;
        public bool IsIfImageNotExistAIItem => _editPanelPropertyService?.IsIfImageNotExistAIItem ?? false;
        public bool IsIfEndItem => _editPanelPropertyService?.IsIfEndItem ?? false;
        public bool IsIfVariableItem => _editPanelPropertyService?.IsIfVariableItem ?? false;
        public bool IsExecuteItem => _editPanelPropertyService?.IsExecuteItem ?? false;
        public bool IsSetVariableItem => _editPanelPropertyService?.IsSetVariableItem ?? false;
        public bool IsSetVariableAIItem => _editPanelPropertyService?.IsSetVariableAIItem ?? false;
        public bool IsScreenshotItem => _editPanelPropertyService?.IsScreenshotItem ?? false;

        public string Comment
        {
            get => _editPanelPropertyService?.GetProperty<string>("Comment") ?? "";
            set
            {
                _messenger.Send(new SetEditPanelPropertyMessage("Comment", value));
                OnPropertyChanged();
            }
        }

        public string WindowTitle
        {
            get => _editPanelPropertyService?.GetProperty<string>("WindowTitle") ?? "";
            set
            {
                _messenger.Send(new SetEditPanelPropertyMessage("WindowTitle", value));
                OnPropertyChanged();
            }
        }

        public string WindowClassName
        {
            get => _editPanelPropertyService?.GetProperty<string>("WindowClassName") ?? "";
            set
            {
                _messenger.Send(new SetEditPanelPropertyMessage("WindowClassName", value));
                OnPropertyChanged();
            }
        }

        public string ImagePath
        {
            get => _editPanelPropertyService?.GetProperty<string>("ImagePath") ?? "";
            set
            {
                _messenger.Send(new SetEditPanelPropertyMessage("ImagePath", value));
                OnPropertyChanged();
            }
        }

        public double Threshold
        {
            get => _editPanelPropertyService?.GetProperty<double>("Threshold", 0.8) ?? 0.8;
            set
            {
                _messenger.Send(new SetEditPanelPropertyMessage("Threshold", value));
                OnPropertyChanged();
            }
        }

        public int Timeout
        {
            get => _editPanelPropertyService?.GetProperty<int>("Timeout", 5000) ?? 5000;
            set
            {
                _messenger.Send(new SetEditPanelPropertyMessage("Timeout", value));
                OnPropertyChanged();
            }
        }

        public int Interval
        {
            get => _editPanelPropertyService?.GetProperty<int>("Interval", 500) ?? 500;
            set
            {
                _messenger.Send(new SetEditPanelPropertyMessage("Interval", value));
                OnPropertyChanged();
            }
        }
    }
}