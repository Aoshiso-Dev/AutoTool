using System;
using System.Linq;
using System.Reflection;
using AutoTool.Core.Abstractions;
using AutoTool.Core.Descriptors;
using AutoTool.Core.Registration;
using AutoTool.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoTool.Desktop.Examples
{
    /// <summary>
    /// Attributeベースのコマンド自動登録のデモ
    /// </summary>
    public static class AttributeCommandRegistrationDemo
    {
        /// <summary>
        /// 自動登録のデモを実行
        /// </summary>
        public static void RunDemo(IServiceProvider services)
        {
            try
            {
                var logger = services.GetService<ILogger<AttributeCommandRegistrationService>>();
                var commandRegistry = services.GetService<ICommandRegistry>();

                if (commandRegistry == null)
                {
                    Console.WriteLine("?? ICommandRegistryが見つかりません");
                    logger?.LogWarning("ICommandRegistryが見つかりません - デモをスキップします");
                    return;
                }

                Console.WriteLine("?? Attributeベースのコマンド自動登録デモを開始します\n");
                logger?.LogInformation("AttributeCommandRegistrationDemo開始");

                // 現在のレジストリに登録されているコマンドを表示
                DisplayRegisteredCommands(commandRegistry, logger);

                // Attribute-based Command Registrationサービスがある場合は追加デモを実行
                var registrationService = services.GetService<AttributeCommandRegistrationService>();
                if (registrationService != null)
                {
                    RunAdvancedDemo(registrationService, commandRegistry, logger);
                }
                else
                {
                    Console.WriteLine("?? AttributeCommandRegistrationServiceが見つかりません");
                    Console.WriteLine("   基本的なコマンドレジストリのみ表示します\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? デモ実行中にエラーが発生しました: {ex.Message}");
                services.GetService<ILogger<AttributeCommandRegistrationService>>()
                    ?.LogError(ex, "AttributeCommandRegistrationDemo実行中にエラー");
            }
        }

        /// <summary>
        /// 登録されているコマンドを表示
        /// </summary>
        private static void DisplayRegisteredCommands(ICommandRegistry registry, ILogger? logger)
        {
            try
            {
                var allDescriptors = registry.All.ToArray();
                
                Console.WriteLine("?? 現在登録されているコマンド一覧:");
                Console.WriteLine("┌──────────┬──────────────┬─────────────┬───────────┐");
                Console.WriteLine("│ Type     │ DisplayName  │ Category    │ Source    │");
                Console.WriteLine("├──────────┼──────────────┼─────────────┼───────────┤");

                foreach (var descriptor in allDescriptors.OrderBy(d => d.Type))
                {
                    var type = descriptor.Type.PadRight(8);
                    var displayName = descriptor.DisplayName.PadRight(12);
                    var category = GetCategory(descriptor).PadRight(11);
                    var source = GetSource(descriptor).PadRight(9);

                    Console.WriteLine($"│ {type} │ {displayName} │ {category} │ {source} │");
                }

                Console.WriteLine("└──────────┴──────────────┴─────────────┴───────────┘");
                Console.WriteLine($"合計: {allDescriptors.Length}個のコマンドが登録されています\n");

                logger?.LogInformation("コマンドレジストリ表示完了: {Count}個", allDescriptors.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? コマンド一覧表示中にエラー: {ex.Message}");
                logger?.LogWarning(ex, "コマンド一覧表示中にエラー");
            }
        }

        /// <summary>
        /// 高度なデモを実行
        /// </summary>
        private static void RunAdvancedDemo(AttributeCommandRegistrationService registrationService, 
            ICommandRegistry registry, ILogger? logger)
        {
            try
            {
                Console.WriteLine("?? 高度なデモ: Attribute-based Command Registration");

                // 対象アセンブリを指定して登録を試行
                var targetAssemblies = new[]
                {
                    Assembly.GetAssembly(typeof(AutoTool.Commands.Flow.Wait.WaitCommand)),
                }.Where(a => a != null).ToArray();

                if (targetAssemblies.Length > 0)
                {
                    Console.WriteLine("?? 対象アセンブリ:");
                    foreach (var assembly in targetAssemblies)
                    {
                        Console.WriteLine($"   - {assembly!.GetName().Name}");
                    }
                    Console.WriteLine();

                    // DynamicCommandRegistryがある場合のみ自動登録を試行
                    if (registry is IDynamicCommandRegistry dynamicRegistry)
                    {
                        var registeredCount = registrationService.RegisterCommandsFromAssemblies(
                            dynamicRegistry, targetAssemblies!);

                        Console.WriteLine($"? 自動登録完了: {registeredCount}個の新しいコマンドが登録されました");
                        logger?.LogInformation("自動登録完了: {Count}個", registeredCount);
                    }
                    else
                    {
                        Console.WriteLine("?? 静的レジストリのため、自動登録はスキップされました");
                    }
                }
                else
                {
                    Console.WriteLine("?? 対象アセンブリが見つかりませんでした");
                }

                // コマンド作成テスト
                TestCommandCreation(registry, logger);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? 高度なデモ中にエラー: {ex.Message}");
                logger?.LogWarning(ex, "高度なデモ中にエラー");
            }
        }

        /// <summary>
        /// コマンド作成テスト
        /// </summary>
        private static void TestCommandCreation(ICommandRegistry registry, ILogger? logger)
        {
            Console.WriteLine("\n?? コマンド作成テスト:");

            var testTypes = new[] { "wait", "click", "if", "keyinput", "while" };

            foreach (var type in testTypes)
            {
                try
                {
                    if (registry.TryGet(type, out var descriptor) && descriptor != null)
                    {
                        var settings = descriptor.CreateDefaultSettings();
                        
                        // サービスプロバイダーが必要な場合はnullを渡す（テスト用）
                        var command = descriptor.CreateCommand(settings, null!);

                        var status = command.IsEnabled ? "有効" : "無効";
                        Console.WriteLine($"   ? {type} → {command.DisplayName} ({status})");
                    }
                    else
                    {
                        Console.WriteLine($"   ? {type} → 見つかりません");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ?? {type} → 作成エラー: {ex.Message}");
                    logger?.LogDebug(ex, "コマンド作成テスト中にエラー: {Type}", type);
                }
            }
            Console.WriteLine();
        }

        /// <summary>
        /// DescriptorからCategoryを取得
        /// </summary>
        private static string GetCategory(ICommandDescriptor descriptor)
        {
            if (descriptor is AttributeBasedDescriptor attrDesc && !string.IsNullOrEmpty(attrDesc.Category))
            {
                return attrDesc.Category;
            }

            // フォールバック: タイプに基づいて推定
            return descriptor.Type switch
            {
                "wait" or "if" or "while" => "フロー制御",
                "click" => "マウス操作", 
                "keyinput" => "キーボード操作",
                _ => "その他"
            };
        }

        /// <summary>
        /// Descriptorのソースを取得
        /// </summary>
        private static string GetSource(ICommandDescriptor descriptor)
        {
            return descriptor is AttributeBasedDescriptor ? "Attribute" : "Manual";
        }
    }

    /// <summary>
    /// 簡単な使用例
    /// </summary>
    public static class QuickStartExample
    {
        public static void ShowUsage()
        {
            Console.WriteLine("?? Attributeベースのコマンド登録 - 使用例\n");

            Console.WriteLine("1?? コマンドクラスにAttributeを追加:");
            Console.WriteLine("""
            [Command("mycommand", "マイコマンド", 
                IconKey = "mdi:star", 
                Category = "カスタム", 
                Description = "説明文",
                Order = 50)]
            public sealed class MyCommand : IAutoToolCommand, IHasSettings<MySettings>
            {
                // 実装...
            }
            """);

            Console.WriteLine("\n2?? 自動登録サービスを使用:");
            Console.WriteLine("""
            var registrationService = new AttributeCommandRegistrationService(logger);
            var count = registrationService.RegisterCommandsFromCurrentDomain(commandRegistry);
            Console.WriteLine($"{count}個のコマンドが自動登録されました");
            """);

            Console.WriteLine("\n3?? メリット:");
            Console.WriteLine("   ? Descriptorクラスが不要");
            Console.WriteLine("   ? 1つのファイルでコマンド定義完結");
            Console.WriteLine("   ? 自動発見・登録");
            Console.WriteLine("   ? メタデータの一元管理");
            Console.WriteLine("   ? タイプセーフ");
            Console.WriteLine();
        }
    }
}