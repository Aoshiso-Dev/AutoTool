using AutoTool.Core.Commands;

namespace AutoTool.Services.Abstractions;

/// <summary>
/// プラグインサービスのインターフェース
/// </summary>
public interface IPluginService
{
    /// <summary>
    /// プラグインディレクトリからプラグインを読み込みます
    /// </summary>
    Task LoadPluginsAsync(string pluginDirectory, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 指定されたプラグインファイルを読み込みます
    /// </summary>
    Task LoadPluginAsync(string pluginFilePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 読み込まれているプラグインの一覧を取得します
    /// </summary>
    IEnumerable<PluginInfo> GetLoadedPlugins();
    
    /// <summary>
    /// 指定された型のコマンドを作成します
    /// </summary>
    IAutoToolCommand? CreateCommand(string commandType);
    
    /// <summary>
    /// 利用可能なコマンド型の一覧を取得します
    /// </summary>
    IEnumerable<string> GetAvailableCommandTypes();
    
    /// <summary>
    /// プラグインをアンロードします
    /// </summary>
    void UnloadPlugin(string pluginName);
    
    /// <summary>
    /// すべてのプラグインをアンロードします
    /// </summary>
    void UnloadAllPlugins();
}

/// <summary>
/// プラグイン情報
/// </summary>
public class PluginInfo
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime LoadedAt { get; set; }
    public IEnumerable<string> CommandTypes { get; set; } = Enumerable.Empty<string>();
}