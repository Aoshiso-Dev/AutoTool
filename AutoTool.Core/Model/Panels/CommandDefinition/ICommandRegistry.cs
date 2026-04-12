using AutoTool.Commands.Interface;
using AutoTool.Panels.Model.List.Interface;

namespace AutoTool.Panels.Model.CommandDefinition;

public enum CommandCreationFailureReason
{
    None = 0,
    MissingItemType,
    UnknownItemType,
    CommandFactoryUnavailable,
    MissingCommandBinding,
    FactoryException
}

public sealed record CommandCreationResult(
    bool Success,
    ICommand? Command,
    CommandCreationFailureReason FailureReason,
    string Message)
{
    public static CommandCreationResult Ok(ICommand command) =>
        new(true, command, CommandCreationFailureReason.None, string.Empty);

    public static CommandCreationResult Fail(CommandCreationFailureReason reason, string message) =>
        new(false, null, reason, message);
}

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
    CommandCreationResult CreateSimple(ICommand parent, ICommandListItem item, IServiceProvider? serviceProvider);

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
/// コマンド定義の参照専用プロバイダー
/// </summary>
public interface ICommandDefinitionProvider
{
    string GetDisplayName(string typeName, string language = "ja");
    string GetCategoryName(string typeName, string language = "ja");
    int GetDisplayPriority(string typeName);
    Type? GetItemType(string typeName);
    bool IsIfCommand(string typeName);
    bool IsLoopCommand(string typeName);
    bool IsEndCommand(string typeName);
    bool IsStartCommand(string typeName);
}


