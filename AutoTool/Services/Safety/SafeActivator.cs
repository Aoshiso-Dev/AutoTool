using System;
using Microsoft.Extensions.Logging;

namespace AutoTool.Services.Safety
{
    /// <summary>
    /// 安全なオブジェクト作成を支援するヘルパークラス
    /// DefaultBinderエラーを回避し、フォールバック機能を提供
    /// </summary>
    public static class SafeActivator
    {
        /// <summary>
        /// 安全なインスタンス作成
        /// </summary>
        public static T? CreateInstance<T>(ILogger? logger = null) where T : class
        {
            try
            {
                logger?.LogDebug("SafeActivator: {Type} の作成を試行します", typeof(T).Name);
                
                // パラメータなしコンストラクタの存在確認
                var constructor = typeof(T).GetConstructor(Type.EmptyTypes);
                if (constructor == null)
                {
                    logger?.LogWarning("SafeActivator: {Type} にパラメータなしコンストラクタが見つかりません", typeof(T).Name);
                    return null;
                }

                // インスタンス作成
                var instance = (T)Activator.CreateInstance(typeof(T));
                logger?.LogDebug("SafeActivator: {Type} の作成に成功しました", typeof(T).Name);
                return instance;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "SafeActivator: {Type} の作成に失敗しました", typeof(T).Name);
                return null;
            }
        }

        /// <summary>
        /// パラメータ付きの安全なインスタンス作成
        /// </summary>
        public static T? CreateInstance<T>(object[] args, ILogger? logger = null) where T : class
        {
            try
            {
                logger?.LogDebug("SafeActivator: {Type} をパラメータ付きで作成を試行します", typeof(T).Name);
                
                // コンストラクタの検索
                var constructors = typeof(T).GetConstructors();
                foreach (var constructor in constructors)
                {
                    var parameters = constructor.GetParameters();
                    if (parameters.Length == args.Length)
                    {
                        // パラメータタイプの一致確認
                        bool isMatch = true;
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            if (args[i] != null && !parameters[i].ParameterType.IsAssignableFrom(args[i].GetType()))
                            {
                                isMatch = false;
                                break;
                            }
                        }

                        if (isMatch)
                        {
                            var instance = (T)Activator.CreateInstance(typeof(T), args);
                            logger?.LogDebug("SafeActivator: {Type} のパラメータ付き作成に成功しました", typeof(T).Name);
                            return instance;
                        }
                    }
                }

                logger?.LogWarning("SafeActivator: {Type} に適合するコンストラクタが見つかりません", typeof(T).Name);
                return null;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "SafeActivator: {Type} のパラメータ付き作成に失敗しました", typeof(T).Name);
                return null;
            }
        }

        /// <summary>
        /// 型の安全性チェック
        /// </summary>
        public static bool IsInstantiable<T>() where T : class
        {
            try
            {
                var type = typeof(T);
                
                // 抽象クラスやインターフェースのチェック
                if (type.IsAbstract || type.IsInterface)
                {
                    return false;
                }

                // パラメータなしコンストラクタの存在確認
                var constructor = type.GetConstructor(Type.EmptyTypes);
                return constructor != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// アプリケーションの起動が可能かどうかをチェック
        /// </summary>
        public static bool CanActivateApplication()
        {
            try
            {
                // 基本的な起動チェック
                // 必要に応じて追加の安全性チェックを実装
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 型の安全なインスタンス化
        /// </summary>
        public static T? CreateInstance<T>(Type type, params object[] args) where T : class
        {
            try
            {
                return Activator.CreateInstance(type, args) as T;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 型の安全なインスタンス化（ジェネリック版）
        /// </summary>
        public static T? CreateInstance<T>(params object[] args) where T : class
        {
            try
            {
                return Activator.CreateInstance<T>();
            }
            catch
            {
                return null;
            }
        }
    }
}