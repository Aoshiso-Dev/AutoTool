namespace AutoTool.Application.Ports;

public interface IUiStatePreferenceStore
{
    bool LoadRestorePreviousSession();
    void SaveRestorePreviousSession(bool enabled);
}
