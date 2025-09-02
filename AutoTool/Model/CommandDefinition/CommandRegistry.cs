using System;
using System.Collections.Generic;
using System.Linq;
using AutoTool.Model.List.Interface;
using AutoTool.Model.List.Type;

namespace AutoTool.Model.CommandDefinition
{
    /// <summary>
    /// Phase 5完全統合版：コマンドレジストリ
    /// MacroPanels依存を削除し、AutoTool統合版のみ使用
    /// </summary>
    public static class CommandRegistry
    {
        private static readonly Dictionary<string, Type> _commandTypes = new();
        private static bool _initialized = false;

        /// <summary>
        /// コマンドタイプの定数（Phase 5統合版）
        /// </summary>
        public static class CommandTypes
        {
            public const string Click = "Click";
            public const string ClickImage = "Click_Image";
            public const string ClickImageAI = "Click_Image_AI";
            public const string Wait = "Wait";
            public const string WaitImage = "Wait_Image";
            public const string Hotkey = "Hotkey";
            public const string Loop = "Loop";
            public const string LoopEnd = "Loop_End";
            public const string LoopBreak = "Loop_Break";
            public const string IfImageExist = "IF_ImageExist";
            public const string IfImageNotExist = "IF_ImageNotExist";
            public const string IfImageExistAI = "IF_ImageExist_AI";
            public const string IfImageNotExistAI = "IF_ImageNotExist_AI";
            public const string IfEnd = "IF_End";
            public const string Execute = "Execute";
            public const string SetVariable = "SetVariable";
            public const string SetVariableAI = "SetVariable_AI";
            public const string IfVariable = "IF_Variable";
            public const string Screenshot = "Screenshot";
            
            // 追加の基本コマンド
            public const string DoubleClick = "DoubleClick";
            public const string RightClick = "RightClick";
            public const string Drag = "Drag";
            public const string Scroll = "Scroll";
            public const string TypeText = "TypeText";
            public const string KeyPress = "KeyPress";
            public const string KeyCombo = "KeyCombo";
            public const string FindImage = "FindImage";
            public const string CaptureImage = "CaptureImage";
            public const string ActivateWindow = "ActivateWindow";
            public const string CloseWindow = "CloseWindow";
            public const string MoveWindow = "MoveWindow";
            public const string ResizeWindow = "ResizeWindow";
            public const string OpenFile = "OpenFile";
            public const string SaveFile = "SaveFile";
            public const string CopyFile = "CopyFile";
            public const string DeleteFile = "DeleteFile";
            public const string IfElse = "IfElse";
            public const string GetVariable = "GetVariable";
            public const string CalculateExpression = "CalculateExpression";
            public const string RandomNumber = "RandomNumber";
            public const string Command = "Command";
            public const string Beep = "Beep";
            public const string HttpRequest = "HttpRequest";
            public const string DownloadFile = "DownloadFile";
            public const string SendEmail = "SendEmail";
            public const string ImageAI = "ImageAI";
            public const string TextOCR = "TextOCR";
            public const string VoiceRecognition = "VoiceRecognition";
            public const string LogMessage = "LogMessage";
            public const string Assert = "Assert";
            public const string Breakpoint = "Breakpoint";
            public const string Comment = "Comment";
        }

        /// <summary>
        /// 表示順序とカテゴリ情報
        /// </summary>
        public static class DisplayOrder
        {
            private static readonly Dictionary<string, (string DisplayName, string Category, int Order)> _displayInfo = new()
            {
                // 基本操作
                { CommandTypes.Wait, ("待機", "基本操作", 10) },
                { CommandTypes.Click, ("クリック", "基本操作", 20) },
                { CommandTypes.DoubleClick, ("ダブルクリック", "基本操作", 21) },
                { CommandTypes.RightClick, ("右クリック", "基本操作", 22) },
                { CommandTypes.Drag, ("ドラッグ", "基本操作", 30) },
                { CommandTypes.Scroll, ("スクロール", "基本操作", 40) },
                
                // キーボード操作
                { CommandTypes.Hotkey, ("ホットキー", "キーボード", 50) },
                { CommandTypes.TypeText, ("テキスト入力", "キーボード", 60) },
                { CommandTypes.KeyPress, ("キー押下", "キーボード", 70) },
                { CommandTypes.KeyCombo, ("キー組み合わせ", "キーボード", 80) },
                
                // 画像認識
                { CommandTypes.WaitImage, ("画像待機", "画像認識", 90) },
                { CommandTypes.ClickImage, ("画像クリック", "画像認識", 100) },
                { CommandTypes.FindImage, ("画像検索", "画像認識", 110) },
                { CommandTypes.CaptureImage, ("画像キャプチャ", "画像認識", 120) },
                
                // ウィンドウ操作
                { CommandTypes.ActivateWindow, ("ウィンドウアクティブ", "ウィンドウ", 130) },
                { CommandTypes.CloseWindow, ("ウィンドウ閉じる", "ウィンドウ", 140) },
                { CommandTypes.MoveWindow, ("ウィンドウ移動", "ウィンドウ", 150) },
                { CommandTypes.ResizeWindow, ("ウィンドウサイズ変更", "ウィンドウ", 160) },
                
                // ファイル操作
                { CommandTypes.OpenFile, ("ファイル開く", "ファイル", 170) },
                { CommandTypes.SaveFile, ("ファイル保存", "ファイル", 180) },
                { CommandTypes.CopyFile, ("ファイルコピー", "ファイル", 190) },
                { CommandTypes.DeleteFile, ("ファイル削除", "ファイル", 200) },
                
                // 制御構造
                { CommandTypes.Loop, ("ループ", "制御", 210) },
                { CommandTypes.LoopEnd, ("ループ終了", "制御", 211) },
                { CommandTypes.LoopBreak, ("ループ中断", "制御", 212) },
                { CommandTypes.IfImageExist, ("画像存在判定", "条件分岐", 220) },
                { CommandTypes.IfImageNotExist, ("画像非存在判定", "条件分岐", 221) },
                { CommandTypes.IfVariable, ("変数条件判定", "条件分岐", 222) },
                { CommandTypes.IfEnd, ("条件終了", "条件分岐", 223) },
                { CommandTypes.IfElse, ("そうでなければ", "条件分岐", 224) },
                
                // 変数・データ
                { CommandTypes.SetVariable, ("変数設定", "変数", 230) },
                { CommandTypes.GetVariable, ("変数取得", "変数", 240) },
                { CommandTypes.CalculateExpression, ("計算式", "変数", 250) },
                { CommandTypes.RandomNumber, ("乱数生成", "変数", 260) },
                
                // システム
                { CommandTypes.Execute, ("プログラム実行", "システム", 270) },
                { CommandTypes.Command, ("コマンド実行", "システム", 280) },
                { CommandTypes.Screenshot, ("スクリーンショット", "システム", 290) },
                { CommandTypes.Beep, ("ビープ音", "システム", 300) },
                
                // ネットワーク
                { CommandTypes.HttpRequest, ("HTTP要求", "ネットワーク", 310) },
                { CommandTypes.DownloadFile, ("ファイルダウンロード", "ネットワーク", 320) },
                { CommandTypes.SendEmail, ("メール送信", "ネットワーク", 330) },
                
                // AI・自動化
                { CommandTypes.ClickImageAI, ("AI画像クリック", "AI", 340) },
                { CommandTypes.IfImageExistAI, ("AI画像存在判定", "AI", 341) },
                { CommandTypes.IfImageNotExistAI, ("AI画像非存在判定", "AI", 342) },
                { CommandTypes.SetVariableAI, ("AI変数設定", "AI", 343) },
                { CommandTypes.ImageAI, ("AI画像認識", "AI", 344) },
                { CommandTypes.TextOCR, ("文字認識", "AI", 350) },
                { CommandTypes.VoiceRecognition, ("音声認識", "AI", 360) },
                
                // デバッグ・テスト
                { CommandTypes.LogMessage, ("ログ出力", "デバッグ", 370) },
                { CommandTypes.Assert, ("アサーション", "デバッグ", 380) },
                { CommandTypes.Breakpoint, ("ブレークポイント", "デバッグ", 390) },
                { CommandTypes.Comment, ("コメント", "デバッグ", 400) }
            };

            public static string GetDisplayName(string typeName)
            {
                return _displayInfo.TryGetValue(typeName, out var info) ? info.DisplayName : typeName;
            }

            public static string GetCategoryName(string typeName)
            {
                return _displayInfo.TryGetValue(typeName, out var info) ? info.Category : "その他";
            }

            public static int GetOrder(string typeName)
            {
                return _displayInfo.TryGetValue(typeName, out var info) ? info.Order : 999;
            }

            public static IEnumerable<string> GetOrderedTypeNames()
            {
                return _displayInfo
                    .OrderBy(kvp => kvp.Value.Order)
                    .ThenBy(kvp => kvp.Value.Category)
                    .ThenBy(kvp => kvp.Value.DisplayName)
                    .Select(kvp => kvp.Key);
            }

            public static IEnumerable<string> GetCategorizedTypeNames()
            {
                return _displayInfo
                    .GroupBy(kvp => kvp.Value.Category)
                    .OrderBy(g => g.Key)
                    .SelectMany(g => g.OrderBy(kvp => kvp.Value.Order).Select(kvp => kvp.Key));
            }
        }

        /// <summary>
        /// CommandRegistryを初期化
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                // BasicCommandItemを基本コマンドタイプとして登録
                _commandTypes.Clear();
                
                // 基本コマンドタイプを登録
                foreach (var typeName in DisplayOrder.GetOrderedTypeNames())
                {
                    _commandTypes[typeName] = typeof(BasicCommandItem);
                }

                _initialized = true;
                System.Diagnostics.Debug.WriteLine($"[CommandRegistry] 初期化完了: {_commandTypes.Count}個のコマンドタイプを登録");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CommandRegistry] 初期化エラー: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// コマンドアイテムを作成
        /// </summary>
        public static ICommandListItem? CreateCommandItem(string typeName)
        {
            try
            {
                if (!_initialized)
                {
                    Initialize();
                }

                if (_commandTypes.TryGetValue(typeName, out var type))
                {
                    if (Activator.CreateInstance(type) is ICommandListItem item)
                    {
                        item.ItemType = typeName;
                        item.Comment = $"{DisplayOrder.GetDisplayName(typeName)}コマンド";
                        item.IsEnable = true;
                        
                        System.Diagnostics.Debug.WriteLine($"[CommandRegistry] コマンドアイテム作成: {typeName}");
                        return item;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[CommandRegistry] 未知のコマンドタイプ: {typeName}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CommandRegistry] コマンドアイテム作成エラー: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 登録されているコマンドタイプ名を順序付きで取得
        /// </summary>
        public static IEnumerable<string> GetOrderedTypeNames()
        {
            if (!_initialized)
            {
                Initialize();
            }

            return DisplayOrder.GetOrderedTypeNames().Where(typeName => _commandTypes.ContainsKey(typeName));
        }

        /// <summary>
        /// カテゴリ別のコマンドタイプ名を取得
        /// </summary>
        public static IEnumerable<(string Category, IEnumerable<string> TypeNames)> GetCategorizedTypeNames()
        {
            if (!_initialized)
            {
                Initialize();
            }

            return DisplayOrder.GetOrderedTypeNames()
                .Where(typeName => _commandTypes.ContainsKey(typeName))
                .GroupBy(typeName => DisplayOrder.GetCategoryName(typeName))
                .OrderBy(g => g.Key)
                .Select(g => (g.Key, g.AsEnumerable()));
        }

        /// <summary>
        /// コマンドタイプが登録されているかチェック
        /// </summary>
        public static bool IsRegistered(string typeName)
        {
            if (!_initialized)
            {
                Initialize();
            }

            return _commandTypes.ContainsKey(typeName);
        }

        /// <summary>
        /// 開始コマンドかどうか判定
        /// </summary>
        public static bool IsStartCommand(string typeName)
        {
            return typeName == CommandTypes.Loop ||
                   IsIfCommand(typeName);
        }

        /// <summary>
        /// 終了コマンドかどうか判定
        /// </summary>
        public static bool IsEndCommand(string typeName)
        {
            return typeName == CommandTypes.LoopEnd ||
                   typeName == CommandTypes.IfEnd;
        }

        /// <summary>
        /// Ifコマンドかどうか判定
        /// </summary>
        public static bool IsIfCommand(string typeName)
        {
            return typeName == CommandTypes.IfImageExist ||
                   typeName == CommandTypes.IfImageNotExist ||
                   typeName == CommandTypes.IfImageExistAI ||
                   typeName == CommandTypes.IfImageNotExistAI ||
                   typeName == CommandTypes.IfVariable;
        }

        /// <summary>
        /// Loopコマンドかどうか判定
        /// </summary>
        public static bool IsLoopCommand(string typeName)
        {
            return typeName == CommandTypes.Loop;
        }

        /// <summary>
        /// 全コマンドタイプを取得
        /// </summary>
        public static IEnumerable<string> GetAllTypeNames()
        {
            return _commandTypes.Keys;
        }
    }
}