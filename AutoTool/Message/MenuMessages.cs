namespace AutoTool.Message
{
    /// <summary>
    /// �v���O�C���ǂݍ��݃��b�Z�[�W
    /// </summary>
    public record LoadPluginMessage(string FilePath);

    /// <summary>
    /// �v���O�C���ēǂݍ��݃��b�Z�[�W
    /// </summary>
    public record RefreshPluginsMessage;

    /// <summary>
    /// �v���O�C�����\�����b�Z�[�W
    /// </summary>
    public record ShowPluginInfoMessage;

    /// <summary>
    /// �p�t�H�[�}���X���X�V���b�Z�[�W
    /// </summary>
    public record RefreshPerformanceMessage;

    /// <summary>
    /// ���O�N���A���b�Z�[�W
    /// </summary>
    public record ClearLogMessage;
}