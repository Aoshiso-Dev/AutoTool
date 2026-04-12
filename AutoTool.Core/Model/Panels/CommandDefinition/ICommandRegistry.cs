using AutoTool.Commands.Interface;
using AutoTool.Panels.Model.List.Interface;

namespace AutoTool.Panels.Model.CommandDefinition;

/// <summary>
/// �R�}���h���W�X�g���̃C���^�[�t�F�[�X
/// </summary>
public interface ICommandRegistry
{
    /// <summary>
    /// ������
    /// </summary>
    void Initialize();

    /// <summary>
    /// ���ׂẴR�}���h�^�C�v����擾
    /// </summary>
    IEnumerable<string> GetAllTypeNames();

    /// <summary>
    /// �����t�����ꂽ�R�}���h�^�C�v����擾
    /// </summary>
    IEnumerable<string> GetOrderedTypeNames();

    /// <summary>
    /// �R�}���h�A�C�e����쐬
    /// </summary>
    ICommandListItem? CreateCommandItem(string typeName);

    /// <summary>
    /// �P���R�}���h��쐬
    /// </summary>
    bool TryCreateSimple(ICommand parent, ICommandListItem item, IServiceProvider? serviceProvider, out ICommand? command);

    /// <summary>
    /// If�n�R�}���h���ǂ�������
    /// </summary>
    bool IsIfCommand(string typeName);

    /// <summary>
    /// ���[�v�n�R�}���h���ǂ�������
    /// </summary>
    bool IsLoopCommand(string typeName);

    /// <summary>
    /// �I���n�R�}���h���ǂ�������
    /// </summary>
    bool IsEndCommand(string typeName);

    /// <summary>
    /// �J�n�n�R�}���h���ǂ�������
    /// </summary>
    bool IsStartCommand(string typeName);

    /// <summary>
    /// �\������擾
    /// </summary>
    string GetDisplayName(string typeName, string language = "ja");

    /// <summary>
    /// �J�e�S������擾
    /// </summary>
    string GetCategoryName(string typeName, string language = "ja");
}

/// <summary>
/// �ÓICommandRegistry����b�v����A�_�v�^
/// </summary>
public class CommandRegistryAdapter : ICommandRegistry
{
    public void Initialize() => CommandRegistry.Initialize();

    public IEnumerable<string> GetAllTypeNames() => CommandRegistry.GetAllTypeNames();

    public IEnumerable<string> GetOrderedTypeNames() => CommandRegistry.GetOrderedTypeNames();

    public ICommandListItem? CreateCommandItem(string typeName) => CommandRegistry.CreateCommandItem(typeName);

    public bool TryCreateSimple(ICommand parent, ICommandListItem item, IServiceProvider? serviceProvider, out ICommand? command) 
        => CommandRegistry.TryCreateSimple(parent, item, serviceProvider, out command);

    public bool IsIfCommand(string typeName) => CommandRegistry.IsIfCommand(typeName);

    public bool IsLoopCommand(string typeName) => CommandRegistry.IsLoopCommand(typeName);

    public bool IsEndCommand(string typeName) => CommandRegistry.IsEndCommand(typeName);

    public bool IsStartCommand(string typeName) => CommandRegistry.IsStartCommand(typeName);

    public string GetDisplayName(string typeName, string language = "ja") 
        => CommandRegistry.DisplayOrder.GetDisplayName(typeName, language);

    public string GetCategoryName(string typeName, string language = "ja") 
        => CommandRegistry.DisplayOrder.GetCategoryName(typeName, language);
}

