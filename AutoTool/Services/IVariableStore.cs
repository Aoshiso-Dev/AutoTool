using System.Collections.Generic;

namespace AutoTool.Services
{
    /// <summary>
    /// 変数ストアのインターフェース（AutoTool版）
    /// </summary>
    public interface IVariableStore
    {
        /// <summary>
        /// 変数を設定
        /// </summary>
        void Set(string name, string value);

        /// <summary>
        /// 変数を取得
        /// </summary>
        string? Get(string name);

        /// <summary>
        /// 全ての変数をクリア
        /// </summary>
        void Clear();

        /// <summary>
        /// 全ての変数を取得
        /// </summary>
        Dictionary<string, string> GetAll();

        /// <summary>
        /// 変数が存在するかチェック
        /// </summary>
        bool Contains(string name);

        /// <summary>
        /// 変数を削除
        /// </summary>
        bool Remove(string name);

        /// <summary>
        /// 変数の数を取得
        /// </summary>
        int Count { get; }
    }
}