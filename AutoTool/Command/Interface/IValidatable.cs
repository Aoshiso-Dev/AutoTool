using System;

namespace AutoTool.Command.Interface
{
    /// <summary>
    /// バリデーション機能を提供するインターフェース
    /// </summary>
    public interface IValidatable
    {
        /// <summary>
        /// 設定値のバリデーションを実行
        /// </summary>
        /// <exception cref="ArgumentException">必須項目が設定されていない場合</exception>
        /// <exception cref="ArgumentOutOfRangeException">値が範囲外の場合</exception>
        void Validate();
    }

    /// <summary>
    /// 範囲チェック用のヘルパークラス
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// 値が0-1の範囲内かチェック
        /// </summary>
        public static void ValidateThreshold(double value, string paramName)
        {
            if (value < 0 || value > 1)
                throw new ArgumentOutOfRangeException(paramName, $"{paramName}は0-1の範囲である必要があります");
        }

        /// <summary>
        /// 文字列が空でないかチェック
        /// </summary>
        public static void ValidateRequired(string value, string paramName, string displayName)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException($"{displayName}は必須です", paramName);
        }

        /// <summary>
        /// 変数名として有効かチェック
        /// </summary>
        public static void ValidateVariableName(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("変数名は必須です", paramName);
        }

        /// <summary>
        /// 正の整数かチェック
        /// </summary>
        public static void ValidatePositiveInteger(int value, string paramName, string displayName)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(paramName, $"{displayName}は1以上である必要があります");
        }
    }
}