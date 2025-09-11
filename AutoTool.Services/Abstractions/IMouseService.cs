namespace AutoTool.Services.Abstractions;

/// <summary>
/// マウス操作サービスのインターフェース
/// </summary>
public interface IMouseService
{
    /// <summary>
    /// 指定された座標でマウスクリックを実行します
    /// </summary>
    Task ClickAsync(int x, int y, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 指定された座標でマウス右クリックを実行します
    /// </summary>
    Task RightClickAsync(int x, int y, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 指定された座標でマウスダブルクリックを実行します
    /// </summary>
    Task DoubleClickAsync(int x, int y, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// マウスカーソルを指定された座標に移動します
    /// </summary>
    Task MoveAsync(int x, int y, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 現在のマウス座標を取得します
    /// </summary>
    (int X, int Y) GetCurrentPosition();
}