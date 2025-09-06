using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AutoTool.Services.Keyboard
{
    /// <summary>
    /// キーボード操作サービスのインターフェース
    /// </summary>
    public interface IKeyboardService
    {
        // 基本キー操作
        /// <summary>
        /// 指定されたキーを押します
        /// </summary>
        /// <param name="key">キー</param>
        void KeyPress(Key key);

        /// <summary>
        /// 修飾キーと組み合わせてキーを押します
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="ctrl">Ctrlキーを押すかどうか</param>
        /// <param name="alt">Altキーを押すかどうか</param>
        /// <param name="shift">Shiftキーを押すかどうか</param>
        void KeyPress(Key key, bool ctrl = false, bool alt = false, bool shift = false);

        /// <summary>
        /// 特定のウィンドウに対してキーを送信します
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="ctrl">Ctrlキーを押すかどうか</param>
        /// <param name="alt">Altキーを押すかどうか</param>
        /// <param name="shift">Shiftキーを押すかどうか</param>
        /// <param name="windowTitle">ウィンドウタイトル</param>
        /// <param name="windowClassName">ウィンドウクラス名</param>
        void KeyPress(Key key, bool ctrl = false, bool alt = false, bool shift = false, string windowTitle = "", string windowClassName = "");

        // 非同期バージョン
        /// <summary>
        /// 指定されたキーを非同期で押します
        /// </summary>
        /// <param name="key">キー</param>
        Task KeyPressAsync(Key key);

        /// <summary>
        /// 修飾キーと組み合わせてキーを非同期で押します
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="ctrl">Ctrlキーを押すかどうか</param>
        /// <param name="alt">Altキーを押すかどうか</param>
        /// <param name="shift">Shiftキーを押すかどうか</param>
        Task KeyPressAsync(Key key, bool ctrl = false, bool alt = false, bool shift = false);

        /// <summary>
        /// 特定のウィンドウに対してキーを非同期で送信します
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="ctrl">Ctrlキーを押すかどうか</param>
        /// <param name="alt">Altキーを押すかどうか</param>
        /// <param name="shift">Shiftキーを押すかどうか</param>
        /// <param name="windowTitle">ウィンドウタイトル</param>
        /// <param name="windowClassName">ウィンドウクラス名</param>
        Task KeyPressAsync(Key key, bool ctrl = false, bool alt = false, bool shift = false, string windowTitle = "", string windowClassName = "");

        // 低レベル操作
        /// <summary>
        /// キーを押下します
        /// </summary>
        /// <param name="key">キー</param>
        void KeyDown(Key key);

        /// <summary>
        /// キーを離します
        /// </summary>
        /// <param name="key">キー</param>
        void KeyUp(Key key);

        // テキスト入力
        /// <summary>
        /// テキストを入力します
        /// </summary>
        /// <param name="text">入力するテキスト</param>
        Task TypeTextAsync(string text);

        /// <summary>
        /// 特定のウィンドウにテキストを入力します
        /// </summary>
        /// <param name="text">入力するテキスト</param>
        /// <param name="windowTitle">ウィンドウタイトル</param>
        /// <param name="windowClassName">ウィンドウクラス名</param>
        Task TypeTextAsync(string text, string windowTitle, string windowClassName = "");

        // キーの状態確認
        /// <summary>
        /// 指定されたキーが押されているかどうかを確認します
        /// </summary>
        /// <param name="key">キー</param>
        /// <returns>キーが押されている場合はtrue</returns>
        bool IsKeyPressed(Key key);

        // ホットキー文字列生成
        /// <summary>
        /// ホットキーの文字列表現を生成します
        /// </summary>
        /// <param name="key">キー</param>
        /// <param name="ctrl">Ctrlキー</param>
        /// <param name="alt">Altキー</param>
        /// <param name="shift">Shiftキー</param>
        /// <returns>ホットキーの文字列表現</returns>
        string GetHotkeyString(Key key, bool ctrl = false, bool alt = false, bool shift = false);
    }
}