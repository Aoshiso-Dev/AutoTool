using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.ViewModel.Shared;
using AutoTool.Command.Base;

namespace AutoTool.Command.Definition
{
    internal static class AutoToolCommandRegistry
    {
        private static readonly ConcurrentDictionary<string, Type> _map = new(StringComparer.OrdinalIgnoreCase);
        private static IServiceProvider? _services;
        private static ILogger? _logger;

        // DisplayOrder helper（公開プロパティとして既存コードから参照される）
        public static class DisplayOrder
        {
            private static readonly ConcurrentDictionary<string, string> _displayNames = new(StringComparer.OrdinalIgnoreCase);

            internal static void Register(string id, Type type)
            {
                if (string.IsNullOrWhiteSpace(id)) return;

                // デフォルト表示名: Command サフィックスを除去したもの
                var name = id;
                if (name.EndsWith("Command", StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(0, name.Length - "Command".Length);

                // CamelCase をスペースで分割（簡易）
                name = System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");

                _displayNames[id] = name;
            }

            public static string? GetDisplayName(string id)
            {
                if (string.IsNullOrEmpty(id)) return id;
                if (_displayNames.TryGetValue(id, out var v)) return v;
                return id;
            }
        }

        /// <summary>
        /// アプリ起動時に1回呼び出す初期化。
        /// Assembly をスキャンして AutoToolCommandAttribute を登録します。
        /// </summary>
        public static void Initialize(IServiceProvider services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            _services = services;
            _logger = services.GetService<ILoggerFactory>()?.CreateLogger("AutoToolCommandRegistry");

            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var asm in assemblies)
                {
                    Type[] types;
                    try { types = asm.GetTypes(); }
                    catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray()!; }

                    foreach (var t in types)
                    {
                        if (t == null) continue;

                        var attrs = t.GetCustomAttributes(typeof(AutoToolCommandAttribute), inherit: false)
                                     .OfType<AutoToolCommandAttribute>();
                        foreach (var a in attrs)
                        {
                            var id = a.CommandId ?? a.Type?.Name ?? t.Name;
                            var targetType = a.Type ?? t;
                            if (!_map.ContainsKey(id))
                            {
                                _map[id] = targetType;
                                DisplayOrder.Register(id, targetType);
                                _logger?.LogDebug("AutoToolCommandRegistry: registered {Id} -> {Type}", id, targetType.FullName);
                            }
                        }
                    }
                }

                // もし attribute を使わずに命名規約で追加したい場合のフォールバック登録（optional）
                // 例: 各コマンドクラスの型名をキーに登録
                foreach (var kv in _map.ToArray()) { /* noop to ensure initialization */ }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "AutoToolCommandRegistry 初期化中に例外");
            }
        }

        /// <summary>
        /// コマンドID から IAutoToolCommand を生成します。見つからなければ null を返します。
        /// parent と serviceProvider はコンストラクタ引数として渡されます。
        /// listItem が与えられた場合、可能ならばその Settings を生成インスタンスのプロパティへ適用します。
        /// </summary>
        public static IAutoToolCommand? CreateCommand(string commandId, IAutoToolCommand? parent = null, UniversalCommandItem? listItem = null, IServiceProvider? serviceProvider = null)
        {
            if (string.IsNullOrEmpty(commandId)) return null;

            if (!_map.TryGetValue(commandId, out var type))
            {
                // 2つ目の試み: コマンドID に "Command" を付けて検索
                if (!_map.TryGetValue(commandId + "Command", out type))
                    return null;
            }

            try
            {
                var sp = serviceProvider ?? _services;
                object? instance = null;

                // コンストラクタ (IAutoToolCommand? parent = null, IServiceProvider? serviceProvider = null)
                var ctor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                               .OrderByDescending(c => c.GetParameters().Length)
                               .FirstOrDefault();

                if (ctor != null)
                {
                    var parms = ctor.GetParameters();
                    var args = new object?[parms.Length];
                    for (int i = 0; i < parms.Length; i++)
                    {
                        var p = parms[i];
                        if (p.ParameterType == typeof(IServiceProvider))
                        {
                            args[i] = sp;
                        }
                        else if (typeof(IAutoToolCommand).IsAssignableFrom(p.ParameterType))
                        {
                            args[i] = parent;
                        }
                        else
                        {
                            // 未知の型には null を渡す
                            args[i] = null;
                        }
                    }

                    instance = ctor.Invoke(args);
                }
                else
                {
                    instance = Activator.CreateInstance(type);
                }

                if (instance is IAutoToolCommand cmd)
                {
                    // listItem.Settings をプロパティへ適用（存在すれば）
                    if (listItem != null)
                    {
                        TryApplySettingsFromListItem(cmd, listItem);
                    }

                    return cmd;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "CreateCommand に失敗: {Id} -> {Type}", commandId, type?.FullName);
                return null;
            }
        }

        /// <summary>
        /// ListPanel 等で使われる UniversalCommandItem の簡易生成。
        /// 完全な定義が必要な場合は既存のファクトリにフォールバックできるよう null を返すことも許容します。
        /// ここでは可能な限り簡易インスタンスを返します。
        /// </summary>
        public static UniversalCommandItem? CreateUniversalItem(string commandId)
        {
            try
            {
                var item = new UniversalCommandItem
                {
                    ItemType = commandId,
                    IsEnable = true,
                    Comment = DisplayOrder.GetDisplayName(commandId) ?? commandId
                };

                // 初期設定定義を持つ場合は呼ぶ（メソッドが存在する想定）
                try
                {
                    var mi = item.GetType().GetMethod("InitializeSettingDefinitions", BindingFlags.Public | BindingFlags.Instance);
                    mi?.Invoke(item, Array.Empty<object>());
                }
                catch { /* ignore */ }

                return item;
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "CreateUniversalItem でエラー（null を返します）: {Id}", commandId);
                return null;
            }
        }

        private static void TryApplySettingsFromListItem(object instance, UniversalCommandItem listItem)
        {
            if (instance == null || listItem == null) return;

            try
            {
                // listItem.Settings を取得（存在する想定）
                var settingsProp = listItem.GetType().GetProperty("Settings", BindingFlags.Public | BindingFlags.Instance);
                if (settingsProp == null) return;

                var settings = settingsProp.GetValue(listItem) as System.Collections.IDictionary;
                if (settings == null) return;

                var targetType = instance.GetType();
                var props = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                      .Where(p => p.CanWrite);

                foreach (System.Collections.DictionaryEntry entry in settings)
                {
                    if (entry.Key == null) continue;
                    var key = entry.Key.ToString()!;
                    var prop = props.FirstOrDefault(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase));
                    if (prop == null) continue;

                    var rawValue = entry.Value;
                    if (rawValue == null)
                    {
                        prop.SetValue(instance, null);
                        continue;
                    }

                    try
                    {
                        var targetTypeProp = prop.PropertyType;
                        object? converted = ConvertValue(rawValue, targetTypeProp);
                        prop.SetValue(instance, converted);
                    }
                    catch
                    {
                        // 個別プロパティの変換失敗は無視
                    }
                }
            }
            catch
            {
                // 失敗しても無視（ログは任意）
            }
        }

        private static object? ConvertValue(object value, Type targetType)
        {
            if (value == null) return null;

            // 既に割り当て可能
            if (targetType.IsAssignableFrom(value.GetType())) return value;

            // Nullable 対応
            var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // JsonElement 対応
            if (value is System.Text.Json.JsonElement je)
            {
                if (underlying == typeof(string)) return je.GetRawText().Trim('"');
                try
                {
                    if (underlying == typeof(int)) return je.GetInt32();
                    if (underlying == typeof(long)) return je.GetInt64();
                    if (underlying == typeof(bool)) return je.GetBoolean();
                    if (underlying == typeof(double)) return je.GetDouble();
                    if (underlying.IsEnum && je.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var s = je.GetString();
                        return Enum.Parse(underlying, s ?? string.Empty);
                    }
                }
                catch { /* fallthrough */ }
            }

            // 文字列から変換
            if (value is string sVal)
            {
                var conv = TypeDescriptor.GetConverter(underlying);
                if (conv != null && conv.CanConvertFrom(typeof(string)))
                {
                    return conv.ConvertFromInvariantString(sVal);
                }
            }

            try
            {
                // 汎用的な変換
                return Convert.ChangeType(value, underlying);
            }
            catch
            {
                // 最後の手段: enum へのパース
                if (underlying.IsEnum)
                {
                    try
                    {
                        return Enum.Parse(underlying, value.ToString() ?? string.Empty);
                    }
                    catch { }
                }
            }

            return null;
        }
    }
}