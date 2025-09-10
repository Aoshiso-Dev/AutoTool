using AutoTool.Core.Abstractions;
using AutoTool.Core.Commands;


namespace AutoTool.Core.Descriptors;


public interface ICommandDescriptor
{
    string Type { get; } // 機械名 ("if", "while" ...)
    string DisplayName { get; } // UI表示
    string? IconKey { get; } // アイコン識別子（任意）


    Type SettingsType { get; } // 強い型の設定
    int LatestSettingsVersion { get; } // 現行スキーマ


    IReadOnlyList<BlockSlot> BlockSlots { get; }


    IAutoToolCommandSettings CreateDefaultSettings();


    /// <summary>古いバージョンの設定を現行へマイグレーション。</summary>
    IAutoToolCommandSettings MigrateToLatest(IAutoToolCommandSettings settings);


    /// <summary>設定（DTO）の単体検証。</summary>
    IEnumerable<string> ValidateSettings(IAutoToolCommandSettings settings);


    /// <summary>実行コマンドのインスタンスを作成。</summary>
    IAutoToolCommand CreateCommand(IAutoToolCommandSettings settings, IServiceProvider services);
}