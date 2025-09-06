using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using AutoTool.Services.ColorPicking;

namespace AutoTool.Services.Capture
{
    /// <summary>
    /// キャプチャサービスのインターフェース（KeyHelper + AdvancedColorPicking機能統合）
    /// </summary>
    public interface ICaptureService
    {
        /// <summary>
        /// 右クリック位置の色を取得
        /// </summary>
        /// <returns>取得された色。キャンセルされたらnull</returns>
        Task<Color?> CaptureColorAtRightClickAsync();

        /// <summary>
        /// スクリーンカラーピッカーを表示して画面上の色を取得します
        /// </summary>
        /// <returns>取得された色、キャンセルされた場合はnull</returns>
        Task<Color?> CaptureColorFromScreenAsync();

        /// <summary>
        /// 現在のマウス位置を取得
        /// </summary>
        /// <returns>マウス位置</returns>
        System.Drawing.Point GetCurrentMousePosition();

        /// <summary>
        /// 現在のマウス位置の色を取得します
        /// </summary>
        /// <returns>マウス位置の色</returns>
        Color GetColorAtCurrentMousePosition();

        /// <summary>
        /// 右クリック位置のウィンドウ情報を取得
        /// </summary>
        /// <returns>ウィンドウ情報。キャンセルされたらnull</returns>
        Task<WindowCaptureResult?> CaptureWindowInfoAtRightClickAsync();

        /// <summary>
        /// 右クリック位置の座標を取得
        /// </summary>
        /// <returns>取得された座標。キャンセルされたらnull</returns>
        Task<System.Drawing.Point?> CaptureCoordinateAtRightClickAsync();

        /// <summary>
        /// キーキャプチャを実行
        /// </summary>
        /// <param name="title">ダイアログタイトル</param>
        /// <returns>キャプチャされたキー情報。キャンセルされたらnull</returns>
        Task<KeyCaptureResult?> CaptureKeyAsync(string title);

        /// <summary>
        /// 指定座標の色を取得
        /// </summary>
        /// <param name="position">座標</param>
        /// <returns>色</returns>
        Color GetColorAt(System.Drawing.Point position);

        /// <summary>
        /// 指定された座標の色を取得します
        /// </summary>
        /// <param name="x">X座標</param>
        /// <param name="y">Y座標</param>
        /// <returns>指定位置の色</returns>
        Color GetColorAt(int x, int y);

        /// <summary>
        /// ColorからHex文字列に変換します
        /// </summary>
        /// <param name="color">変換する色</param>
        /// <returns>Hex文字列（例: #FF0000）</returns>
        string ColorToHex(Color color);

        /// <summary>
        /// Hex文字列からColorに変換します
        /// </summary>
        /// <param name="hex">Hex文字列（例: #FF0000 または FF0000）</param>
        /// <returns>変換された色</returns>
        Color? HexToColor(string hex);

        /// <summary>
        /// System.Drawing.ColorからSystem.Windows.Media.Colorに変換します
        /// </summary>
        /// <param name="drawingColor">変換元の色</param>
        /// <returns>変換された色</returns>
        System.Windows.Media.Color ToMediaColor(Color drawingColor);

        /// <summary>
        /// System.Windows.Media.ColorからSystem.Drawing.Colorに変換します
        /// </summary>
        /// <param name="mediaColor">変換元の色</param>
        /// <returns>変換された色</returns>
        Color ToDrawingColor(System.Windows.Media.Color mediaColor);

        /// <summary>
        /// カラーピッカーが現在アクティブかどうかを取得します
        /// </summary>
        bool IsColorPickerActive { get; }

        /// <summary>
        /// カラーピッカーをキャンセルします
        /// </summary>
        void CancelColorPicker();

        // ======== AdvancedColorPicking統合機能 ========

        /// <summary>
        /// AdvancedColorPickingの色履歴
        /// </summary>
        IReadOnlyList<ColorInfo> ColorHistory { get; }

        /// <summary>
        /// 最後に取得した色情報
        /// </summary>
        ColorInfo? LastColorInfo { get; }

        /// <summary>
        /// 高度なカラーピッキング: 指定座標の色を取得
        /// </summary>
        /// <param name="x">X座標</param>
        /// <param name="y">Y座標</param>
        /// <returns>取得した色情報</returns>
        Task<ColorInfo?> GetColorInfoAtPositionAsync(int x, int y);

        /// <summary>
        /// 高度なカラーピッキング: 現在のマウス位置の色を取得
        /// </summary>
        /// <returns>マウス位置の色情報</returns>
        Task<ColorInfo?> GetColorInfoAtCurrentMousePositionAsync();

        /// <summary>
        /// 高度なカラーピッキング: 指定領域の平均色を取得
        /// </summary>
        /// <param name="region">取得領域</param>
        /// <returns>平均色情報</returns>
        Task<ColorInfo?> GetAverageColorInRegionAsync(Rect region);

        /// <summary>
        /// 高度なカラーピッキング: 類似色検索
        /// </summary>
        /// <param name="targetColor">検索対象の色</param>
        /// <param name="tolerance">許容差（0-100）</param>
        /// <returns>見つかった色の位置</returns>
        Task<ColorInfo?> FindSimilarColorAsync(Color targetColor, double tolerance = 10.0);

        /// <summary>
        /// 高度なカラーピッキング: 色ヒストグラム解析
        /// </summary>
        /// <param name="region">解析領域</param>
        /// <returns>色ヒストグラム情報</returns>
        Task<ColorHistogram?> GetColorHistogramAsync(Rect region);

        /// <summary>
        /// AdvancedColorPicking統計情報を取得
        /// </summary>
        /// <returns>統計情報</returns>
        ColorHistoryStatistics GetColorHistoryStatistics();

        /// <summary>
        /// 色履歴をクリア
        /// </summary>
        void ClearColorHistory();

        /// <summary>
        /// 履歴から色パレットを生成
        /// </summary>
        /// <param name="maxColors">最大色数</param>
        /// <returns>色パレット</returns>
        IEnumerable<Color> GenerateColorPalette(int maxColors = 16);

        /// <summary>
        /// 補色パレットを生成
        /// </summary>
        /// <param name="baseColor">ベース色</param>
        /// <returns>補色パレット</returns>
        IEnumerable<Color> GenerateComplementaryPalette(Color baseColor);

        // ======== KeyHelper統合機能 ========

        /// <summary>
        /// グローバルキーを送信します
        /// </summary>
        /// <param name="key">送信するキー</param>
        void SendGlobalKey(Key key);

        /// <summary>
        /// グローバルキーを送信します（修飾キー付き）
        /// </summary>
        /// <param name="key">送信するキー</param>
        /// <param name="ctrl">Ctrlキーを押すかどうか</param>
        /// <param name="alt">Altキーを押すかどうか</param>
        /// <param name="shift">Shiftキーを押すかどうか</param>
        void SendGlobalKey(Key key, bool ctrl = false, bool alt = false, bool shift = false);

        /// <summary>
        /// 指定ウィンドウにキーを送信します
        /// </summary>
        /// <param name="key">送信するキー</param>
        /// <param name="windowTitle">ウィンドウタイトル</param>
        /// <param name="windowClassName">ウィンドウクラス名</param>
        void SendKeyToWindow(Key key, string windowTitle = "", string windowClassName = "");

        /// <summary>
        /// 指定ウィンドウにキーを送信します（修飾キー付き）
        /// </summary>
        /// <param name="key">送信するキー</param>
        /// <param name="ctrl">Ctrlキーを押すかどうか</param>
        /// <param name="alt">Altキーを押すかどうか</param>
        /// <param name="shift">Shiftキーを押すかどうか</param>
        /// <param name="windowTitle">ウィンドウタイトル</param>
        /// <param name="windowClassName">ウィンドウクラス名</param>
        void SendKeyToWindow(Key key, bool ctrl = false, bool alt = false, bool shift = false, string windowTitle = "", string windowClassName = "");

        /// <summary>
        /// グローバルキーを非同期で送信します
        /// </summary>
        /// <param name="key">送信するキー</param>
        /// <param name="ctrl">Ctrlキーを押すかどうか</param>
        /// <param name="alt">Altキーを押すかどうか</param>
        /// <param name="shift">Shiftキーを押すかどうか</param>
        Task SendGlobalKeyAsync(Key key, bool ctrl = false, bool alt = false, bool shift = false);

        /// <summary>
        /// 指定ウィンドウにキーを非同期で送信します
        /// </summary>
        /// <param name="key">送信するキー</param>
        /// <param name="ctrl">Ctrlキーを押すかどうか</param>
        /// <param name="alt">Altキーを押すかどうか</param>
        /// <param name="shift">Shiftキーを押すかどうか</param>
        /// <param name="windowTitle">ウィンドウタイトル</param>
        /// <param name="windowClassName">ウィンドウクラス名</param>
        Task SendKeyToWindowAsync(Key key, bool ctrl = false, bool alt = false, bool shift = false, string windowTitle = "", string windowClassName = "");

        /// <summary>
        /// 連続でキーを送信します
        /// </summary>
        /// <param name="keys">送信するキーのリスト</param>
        /// <param name="intervalMs">キー間の間隔（ミリ秒）</param>
        Task SendKeySequenceAsync(IEnumerable<Key> keys, int intervalMs = 100);

        /// <summary>
        /// 文字列として文字を送信します
        /// </summary>
        /// <param name="text">送信するテキスト</param>
        /// <param name="intervalMs">文字間の間隔（ミリ秒）</param>
        Task SendTextAsync(string text, int intervalMs = 50);

        /// <summary>
        /// 指定ウィンドウに文字列を送信します
        /// </summary>
        /// <param name="text">送信するテキスト</param>
        /// <param name="windowTitle">ウィンドウタイトル</param>
        /// <param name="windowClassName">ウィンドウクラス名</param>
        /// <param name="intervalMs">文字間の間隔（ミリ秒）</param>
        Task SendTextToWindowAsync(string text, string windowTitle = "", string windowClassName = "", int intervalMs = 50);

        /// <summary>
        /// KeyHelperサービスが現在アクティブかどうかを取得します
        /// </summary>
        bool IsKeyHelperActive { get; }

        /// <summary>
        /// KeyHelper処理をキャンセルします
        /// </summary>
        void CancelKeyHelper();
    }
}