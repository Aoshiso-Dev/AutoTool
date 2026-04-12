namespace AutoTool.Panels.Attributes;

/// <summary>
/// �R�}���h�v���p�e�B��UI����t�^���鑮��
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class CommandPropertyAttribute : Attribute
{
    /// <summary>�\����</summary>
    public string DisplayName { get; }
    
    /// <summary>�G�f�B�^�̎��</summary>
    public EditorType EditorType { get; }
    
    /// <summary>�O���[�v���i�����O���[�v�͓����J�[�h�ɕ\���j</summary>
    public string Group { get; set; } = "��{�ݒ�";
    
    /// <summary>�\�������i�O���[�v��ł̏����j</summary>
    public int Order { get; set; } = 0;
    
    /// <summary>�����</summary>
    public string? Description { get; set; }
    
    /// <summary>�ŏ��l�iNumberBox, Slider�p�j</summary>
    public double Min { get; set; } = 0;
    
    /// <summary>�ő�l�iNumberBox, Slider�p�j</summary>
    public double Max { get; set; } = double.MaxValue;
    
    /// <summary>�X�e�b�v�l�iSlider�p�j</summary>
    public double Step { get; set; } = 1;
    
    /// <summary>�P�ʁi�\���p�j</summary>
    public string? Unit { get; set; }
    
    /// <summary>�R���{�{�b�N�X�̑I����i�J���}��؂�j</summary>
    public string? Options { get; set; }
    
    /// <summary>�t�@�C���t�B���^�[�iFilePicker�p�j</summary>
    public string? FileFilter { get; set; }
    
    /// <summary>
    /// �R�}���h�v���p�e�B������쐬
    /// </summary>
    /// <param name="displayName">�\����</param>
    /// <param name="editorType">�G�f�B�^�̎��</param>
    public CommandPropertyAttribute(string displayName, EditorType editorType)
    {
        DisplayName = displayName;
        EditorType = editorType;
    }
}

