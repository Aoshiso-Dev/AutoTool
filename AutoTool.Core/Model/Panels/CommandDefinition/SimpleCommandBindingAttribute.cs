using System;

namespace AutoTool.Panels.Model.CommandDefinition;

/// <summary>
/// �P���ȃR�}���h�o�C���f�B���O�p�̑���
/// ���̑������t�����A�C�e���́A�����I�ɃR�}���h�t�@�N�g���ŏ��������
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SimpleCommandBindingAttribute : Attribute
{
    /// <summary>
    /// �R�}���h�����N���X�^
    /// </summary>
    public Type CommandType { get; }
    
    /// <summary>
    /// �ݒ�C���^�[�t�F�[�X�^
    /// </summary>
    public Type SettingsType { get; }

    public SimpleCommandBindingAttribute(Type commandType, Type settingsType)
    {
        CommandType = commandType;
        SettingsType = settingsType;
    }
}

