namespace AutoTool.Application.Ports;

/// <summary>
/// UI 状態復元の設定値を保存・読み込みするポートです。
/// </summary>
public interface IUiStatePreferenceStore
{
    /// <summary>前回セッションを復元する設定値を読み込みます。</summary>
    bool LoadRestorePreviousSession();
    /// <summary>前回セッション復元の設定値を保存します。</summary>
    void SaveRestorePreviousSession(bool enabled);
}
