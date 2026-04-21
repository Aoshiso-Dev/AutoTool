using AutoTool.Automation.Runtime.Definitions;

namespace AutoTool.Desktop.Panels.ViewModel.Shared;

/// <summary>
/// コマンド一覧に表示する説明文を提供します。
/// </summary>
public static class CommandDescriptionCatalog
{
    public static string GetDescription(string typeName)
    {
        return typeName switch
        {
            CommandTypeNames.Click => "指定した座標や対象ウィンドウでマウスクリックを実行します。",
            CommandTypeNames.ClickImage => "画像を検索し、見つかった位置をクリックします。",
            CommandTypeNames.ClickImageAI => "AIモデルで対象を検出し、検出位置をクリックします。",
            CommandTypeNames.Hotkey => "指定したキーの組み合わせを送信します。",
            CommandTypeNames.Wait => "指定した時間だけ待機します。",
            CommandTypeNames.WaitImage => "指定画像が見つかるまで待機します。",
            CommandTypeNames.WaitImageDisappear => "指定画像が画面から消えるまで待機します。",
            CommandTypeNames.Execute => "外部プログラムやコマンドを実行します。",
            CommandTypeNames.Screenshot => "画面やウィンドウをキャプチャして画像保存します。",
            CommandTypeNames.SetVariable => "固定値や実行結果を変数に設定します。",
            CommandTypeNames.SetVariableAI => "AI検出結果を変数に保存します。",
            CommandTypeNames.SetVariableOCR => "OCRで読み取った文字や信頼度を変数に保存します。",
            CommandTypeNames.FindImage => "画像の有無を判定し、結果を次処理へ渡します。",
            CommandTypeNames.FindText => "OCRで文字列の一致を判定し、結果を次処理へ渡します。",
            CommandTypeNames.IfImageExist => "画像が見つかったときだけ内側の処理を実行します。",
            CommandTypeNames.IfImageNotExist => "画像が見つからないときだけ内側の処理を実行します。",
            CommandTypeNames.IfTextExist => "文字列が見つかったときだけ内側の処理を実行します。",
            CommandTypeNames.IfTextNotExist => "文字列が見つからないときだけ内側の処理を実行します。",
            CommandTypeNames.IfImageExistAI => "AI検出で対象が見つかったときだけ内側の処理を実行します。",
            CommandTypeNames.IfImageNotExistAI => "AI検出で対象が見つからないときだけ内側の処理を実行します。",
            CommandTypeNames.IfVariable => "変数の比較条件が成立したときだけ内側の処理を実行します。",
            CommandTypeNames.IfEnd => "条件分岐ブロックの終了位置を示します。",
            CommandTypeNames.Loop => "指定回数または無限で処理を繰り返します。",
            CommandTypeNames.LoopBreak => "現在のループを中断して抜けます。",
            CommandTypeNames.LoopEnd => "ループブロックの終了位置を示します。",
            CommandTypeNames.Retry => "内側の処理を失敗時に再試行します。",
            CommandTypeNames.RetryEnd => "再試行ブロックの終了位置を示します。",
            _ => "このコマンドの説明はまだ登録されていません。"
        };
    }
}
