using System;
using System.Collections.Generic;

namespace AutoTool.Application.Ports;

/// <summary>
/// アプリケーションログ出力を提供するポートです。
/// </summary>
public interface ILogWriter
{
    /// <summary>文字列メッセージをログへ出力します。</summary>
    void Write(params string[] messages);
    /// <summary>構造化ログとしてカテゴリ・イベント名・フィールドを出力します。</summary>
    void WriteStructured(string category, string eventName, IReadOnlyDictionary<string, object?> fields);
    /// <summary>例外情報をログへ出力します。</summary>
    void Write(Exception exception);
}
