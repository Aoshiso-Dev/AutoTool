// �V�����R�}���h�ǉ��e���v���[�g
// ���̃t�@�C�����R�s�[���ĐV�����R�}���h���ȒP�ɒǉ��ł��܂�

using CommunityToolkit.Mvvm.ComponentModel;
using MacroPanels.List.Class;
using MacroPanels.Model.List.Interface;
using MacroPanels.Command.Interface;
using MacroPanels.Command.Class;
using MacroPanels.Model.CommandDefinition;

namespace MacroPanels.List.Class
{
    // ============================================
    // �V�����R�}���h�̒ǉ��菇:
    // 1. ���L�̃e���v���[�g���R�s�[
    // 2. �N���X����ύX�i��FMyNewCommandItem�j
    // 3. CommandDefinition�����̃p�����[�^��ύX
    // 4. �K�v�ȃv���p�e�B��ǉ�
    // 5. Description�v���p�e�B������
    // 
    // �����ňȉ�����������܂��F
    // - ItemType.GetTypes()�Ɏ����ǉ�
    // - EditPanelViewModel�̑I�����X�g�Ɏ����ǉ�
    // - �t�@�N�g���ł̎��������Ή�
    // - �V���A���C�[�[�V�����Ή�
    // ============================================

    /*
    /// <summary>
    /// �V�����R�}���h�̃A�C�e��
    /// </summary>
    [CommandDefinition("MyNewCommand", typeof(MyNewCommand), typeof(IMyNewCommandSettings), CommandCategory.Action)]
    public partial class MyNewCommandItem : CommandListItem, IMyNewCommandItem, IMyNewCommandSettings
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Description))]
        private string _myProperty = string.Empty;

        // Description�v���p�e�B�͕K�{
        public new string Description => $"MyNewCommand: {MyProperty}";

        public MyNewCommandItem() { }

        public MyNewCommandItem(MyNewCommandItem? item = null) : base(item)
        {
            if (item != null)
            {
                MyProperty = item.MyProperty;
            }
        }

        public new ICommandListItem Clone()
        {
            return new MyNewCommandItem(this);
        }
    }
    */

    /*
    // �Ή�����C���^�[�t�F�[�X��Command.Interface�ɒǉ�
    public interface IMyNewCommandItem : ICommandListItem
    {
        string MyProperty { get; set; }
    }

    public interface IMyNewCommandSettings : ICommandSettings
    {
        string MyProperty { get; set; }
    }

    public interface IMyNewCommand : ICommand
    {
        new IMyNewCommandSettings Settings { get; }
    }
    */

    /*
    // �Ή�����Command������Command.Class�ɒǉ�
    public class MyNewCommand : BaseCommand, ICommand, IMyNewCommand
    {
        new public IMyNewCommandSettings Settings => (IMyNewCommandSettings)base.Settings;

        public MyNewCommand(ICommand parent, ICommandSettings settings) : base(parent, settings) { }

        protected override async Task<bool> DoExecuteAsync(CancellationToken cancellationToken)
        {
            // �R�}���h�̎���
            OnDoingCommand?.Invoke(this, $"MyNewCommand�����s���܂���: {Settings.MyProperty}");
            return true;
        }
    }
    */
}