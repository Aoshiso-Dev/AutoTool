using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoTool.ViewModel;
using AutoTool.ViewModel.Panels;
using AutoTool.Services;
using AutoTool.Services.Mouse;
using AutoTool.Services.Keyboard;
using AutoTool.Services.Capture;
using AutoTool.Services.Configuration;
using AutoTool.Services.Plugin;
using AutoTool.Services.UI;
using AutoTool.Services.ImageProcessing;
using CommunityToolkit.Mvvm.Messaging;
using AutoTool.Command.Definition;
using AutoTool.ViewModel.Shared;
using AutoTool.Services.ColorPicking;

namespace AutoTool.Services
{
    /// <summary>
    /// サービスコレクション拡張メソッド
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// AutoToolの全サービスを登録
        /// </summary>
        public static IServiceCollection AddAutoToolServices(this IServiceCollection services)
        {
            // ロギング設定
            services.AddLogging(builder =>
            {
                builder.AddDebug();
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Messaging設定
            services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

            // Configuration Services
            services.AddSingleton<IEnhancedConfigurationService, EnhancedConfigurationService>();

            // Core Services
            services.AddSingleton<RecentFileService, RecentFileService>();

            // Plugin Services
            services.AddSingleton<IPluginService, PluginService>();

            // UI Services
            services.AddTransient<IMainWindowMenuService, MainWindowMenuService>();

            // Variable Store Services
            services.AddSingleton<IVariableStoreService, VariableStoreService>();

            // Mouse Services
            services.AddSingleton<IMouseService, MouseService>();

            // Keyboard Services
            services.AddSingleton<IKeyboardService, KeyboardService>();

            // Capture Services (includes ColorPick + KeyHelper functionality)
            services.AddSingleton<ICaptureService, CaptureService>();

            // In-memory log message service for detailed UI log tab
            services.AddSingleton<AutoTool.Services.Logging.LogMessageService>();

            // Image Processing Services (OpenCVHelper統合・WPF対応)
            services.AddSingleton<IImageProcessingService, ImageProcessingService>();

            // ColorPick Service Alias (maps to CaptureService)
            services.AddSingleton<IColorPickService>(provider => provider.GetRequiredService<ICaptureService>() as IColorPickService ?? new ColorPickServiceAdapter(provider.GetRequiredService<ICaptureService>()));

            // Advanced Color Picking Service (for Color History and Enhanced Features)
            services.AddSingleton<IAdvancedColorPickingService, AdvancedColorPickingService>();

            // Macro Execution Services
            services.AddSingleton<AutoTool.Services.Execution.IMacroExecutionService, AutoTool.Services.Execution.MacroExecutionService>();

            // KeyHelper Service Alias (maps to CaptureService)
            services.AddSingleton<IKeyHelperService>(provider => provider.GetRequiredService<ICaptureService>() as IKeyHelperService ?? new KeyHelperServiceAdapter(provider.GetRequiredService<ICaptureService>()));

            // OpenCVHelper Service Alias (maps to ImageProcessingService)
            services.AddSingleton<IOpenCVHelperService>(provider => new OpenCVHelperServiceAdapter(provider.GetRequiredService<IImageProcessingService>()));

            // Window Services
            services.AddSingleton<AutoTool.Services.Window.IWindowInfoService, AutoTool.Services.Window.WindowInfoService>();
            
            // ViewModels (全体で単一インスタンスを共有するため Singleton 登録に変更)
            // 以前は Transient だったため、MainWindow と各パネル設定時に複数回生成され Messenger に多重登録されていた。
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<ListPanelViewModel>();
            services.AddSingleton<EditPanelViewModel>();
            services.AddSingleton<ButtonPanelViewModel>();

            // Command List Item Factory
            services.AddSingleton<IUniversalCommandItemFactory, UniversalCommandItemFactory>();

            // Undo/Redo System
            services.AddSingleton<AutoTool.ViewModel.Shared.CommandHistoryManager>();

            // Dummy services for missing dependencies
            services.AddSingleton<IDataContextLocator, DataContextLocator>();

            // ファイルサービス
            services.AddSingleton<RecentFileService>();
            services.AddScoped<AutoTool.Services.MacroFile.IMacroFileService, AutoTool.Services.MacroFile.MacroFileService>();

            return services;
        }
    }

    /// <summary>
    /// ColorPickサービスインターフェース（CaptureServiceの機能拡張）
    /// </summary>
    public interface IColorPickService
    {
        /// <summary>
        /// スクリーンカラーピッカーを表示して画面上の色を取得します
        /// </summary>
        /// <returns>取得された色、キャンセルされた場合はnull</returns>
        Task<System.Drawing.Color?> CaptureColorFromScreenAsync();

        /// <summary>
        /// 指定された座標の色を取得します
        /// </summary>
        /// <param name="x">X座標</param>
        /// <param name="y">Y座標</param>
        /// <returns>指定位置の色</returns>
        System.Drawing.Color GetColorAt(int x, int y);

        /// <summary>
        /// 指定された座標の色を取得します
        /// </summary>
        /// <param name="point">座標</param>
        /// <returns>指定位置の色</returns>
        System.Drawing.Color GetColorAt(System.Drawing.Point point);

        /// <summary>
        /// 現在のマウス位置の色を取得します
        /// </summary>
        /// <returns>マウス位置の色</returns>
        System.Drawing.Color GetColorAtCurrentMousePosition();

        /// <summary>
        /// ColorからHex文字列に変換します
        /// </summary>
        /// <param name="color">変換する色</param>
        /// <returns>Hex文字列（例: #FF0000）</returns>
        string ColorToHex(System.Drawing.Color color);

        /// <summary>
        /// Hex文字列からColorに変換します
        /// </summary>
        /// <param name="hex">Hex文字列（例: #FF0000 または FF0000）</param>
        /// <returns>変換された色</returns>
        System.Drawing.Color? HexToColor(string hex);

        /// <summary>
        /// System.Drawing.ColorからSystem.Windows.Media.Colorに変換します
        /// </summary>
        /// <param name="drawingColor">変換元の色</param>
        /// <returns>変換された色</returns>
        System.Windows.Media.Color ToMediaColor(System.Drawing.Color drawingColor);

        /// <summary>
        /// System.Windows.Media.ColorからSystem.Drawing.Colorに変換します
        /// </summary>
        /// <param name="mediaColor">変換元の色</param>
        /// <returns>変換された色</returns>
        System.Drawing.Color ToDrawingColor(System.Windows.Media.Color mediaColor);

        /// <summary>
        /// カラーピッカーが現在アクティブかどうかを取得します
        /// </summary>
        bool IsColorPickerActive { get; }

        /// <summary>
        /// カラーピッカーをキャンセルします
        /// </summary>
        void CancelColorPicker();
    }

    /// <summary>
    /// KeyHelperサービスインターフェース（CaptureServiceの機能拡張）
    /// </summary>
    public interface IKeyHelperService
    {
        /// <summary>
        /// グローバルキーを送信します
        /// </summary>
        /// <param name="key">送信するキー</param>
        void SendGlobalKey(System.Windows.Input.Key key);

        /// <summary>
        /// グローバルキーを送信します（修飾キー付き）
        /// </summary>
        /// <param name="key">送信するキー</param>
        /// <param name="ctrl">Ctrlキーを押すかどうか</param>
        /// <param name="alt">Altキーを押すかどうか</param>
        /// <param name="shift">Shiftキーを押すかどうか</param>
        void SendGlobalKey(System.Windows.Input.Key key, bool ctrl = false, bool alt = false, bool shift = false);

        /// <summary>
        /// 指定ウィンドウにキーを送信します
        /// </summary>
        /// <param name="key">送信するキー</param>
        /// <param name="windowTitle">ウィンドウタイトル</param>
        /// <param name="windowClassName">ウィンドウクラス名</param>
        void SendKeyToWindow(System.Windows.Input.Key key, string windowTitle = "", string windowClassName = "");

        /// <summary>
        /// 指定ウィンドウにキーを送信します（修飾キー付き）
        /// </summary>
        /// <param name="key">送信するキー</param>
        /// <param name="ctrl">Ctrlキーを押すかどうか</param>
        /// <param name="alt">Altキーを押すかどうか</param>
        /// <param name="shift">Shiftキーを押すかどうか</param>
        /// <param name="windowTitle">ウィンドウタイトル</param>
        /// <param name="windowClassName">ウィンドウクラス名</param>
        void SendKeyToWindow(System.Windows.Input.Key key, bool ctrl = false, bool alt = false, bool shift = false, string windowTitle = "", string windowClassName = "");

        /// <summary>
        /// グローバルキーを非同期で送信します
        /// </summary>
        /// <param name="key">送信するキー</param>
        /// <param name="ctrl">Ctrlキーを押すかどうか</param>
        /// <param name="alt">Altキーを押すかどうか</param>
        /// <param name="shift">Shiftキーを押すかどうか</param>
        Task SendGlobalKeyAsync(System.Windows.Input.Key key, bool ctrl = false, bool alt = false, bool shift = false);

        /// <summary>
        /// 指定ウィンドウにキーを非同期で送信します
        /// </summary>
        /// <param name="key">送信するキー</param>
        /// <param name="ctrl">Ctrlキーを押すかどうか</param>
        /// <param name="alt">Altキーを押すかどうか</param>
        /// <param name="shift">Shiftキーを押すかどうか</param>
        /// <param name="windowTitle">ウィンドウタイトル</param>
        /// <param name="windowClassName">ウィンドウクラス名</param>
        Task SendKeyToWindowAsync(System.Windows.Input.Key key, bool ctrl = false, bool alt = false, bool shift = false, string windowTitle = "", string windowClassName = "");

        /// <summary>
        /// 連続でキーを送信します
        /// </summary>
        /// <param name="keys">送信するキーのリスト</param>
        /// <param name="intervalMs">キー間の間隔（ミリ秒）</param>
        Task SendKeySequenceAsync(IEnumerable<System.Windows.Input.Key> keys, int intervalMs = 100);

        /// <summary>
        /// 文字列として文字を送信します
        /// </summary>
        /// <param name="text">送信するテキスト</param>
        /// <param name="intervalMs">文字間の間隔（ミリ秒）</param>
        Task SendTextAsync(string text, int intervalMs = 50);

        /// <summary>
        /// 指定ウィンドウに文字列を送信します
        /// </summary>
        /// <param name="text">送信するテキスト</param>
        /// <param name="windowTitle">ウィンドウタイトル</param>
        /// <param name="windowClassName">ウィンドウクラス名</param>
        /// <param name="intervalMs">文字間の間隔（ミリ秒）</param>
        Task SendTextToWindowAsync(string text, string windowTitle = "", string windowClassName = "", int intervalMs = 50);

        /// <summary>
        /// KeyHelperサービスが現在アクティブかどうかを取得します
        /// </summary>
        bool IsKeyHelperActive { get; }

        /// <summary>
        /// KeyHelper処理をキャンセルします
        /// </summary>
        void CancelKeyHelper();
    }

    /// <summary>
    /// OpenCVHelperサービスインターフェース（ImageProcessingServiceの機能拡張）
    /// </summary>
    public interface IOpenCVHelperService
    {
        /// <summary>
        /// スクリーン全体をキャプチャします
        /// </summary>
        /// <returns>キャプチャされた画像</returns>
        Task<OpenCvSharp.Mat?> CaptureScreenAsync();

        /// <summary>
        /// 指定されたウィンドウをキャプチャします
        /// </summary>
        /// <param name="windowTitle">ウィンドウタイトル</param>
        /// <param name="windowClassName">ウィンドウクラス名（オプション）</param>
        /// <returns>キャプチャされた画像</returns>
        Task<OpenCvSharp.Mat?> CaptureWindowAsync(string windowTitle, string windowClassName = "");

        /// <summary>
        /// 画像検索を実行します
        /// </summary>
        /// <param name="imagePath">検索する画像のパス</param>
        /// <param name="threshold">マッチング閾値</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>発見された位置、見つからない場合はnull</returns>
        Task<System.Windows.Point?> SearchImageAsync(string imagePath, double threshold = 0.8, CancellationToken cancellationToken = default);

        /// <summary>
        /// ウィンドウ内で画像検索を実行します
        /// </summary>
        /// <param name="imagePath">検索する画像のパス</param>
        /// <param name="windowTitle">検索対象のウィンドウタイトル</param>
        /// <param name="windowClassName">検索対象のウィンドウクラス名（オプション）</param>
        /// <param name="threshold">マッチング閾値</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>発見された位置、見つからない場合はnull</returns>
        Task<System.Windows.Point?> SearchImageInWindowAsync(string imagePath, string windowTitle, string windowClassName = "", double threshold = 0.8, CancellationToken cancellationToken = default);

        /// <summary>
        /// 画像をファイルに保存します
        /// </summary>
        /// <param name="image">保存する画像</param>
        /// <param name="filePath">保存先ファイルパス</param>
        /// <returns>保存が成功したかどうか</returns>
        Task<bool> SaveImageAsync(OpenCvSharp.Mat image, string filePath);

        /// <summary>
        /// 処理が現在アクティブかどうかを取得します
        /// </summary>
        bool IsProcessingActive { get; }

        /// <summary>
        /// 処理をキャンセルします
        /// </summary>
        void CancelProcessing();
    }

    /// <summary>
    /// CaptureServiceからIColorPickServiceへのアダプター
    /// </summary>
    public class ColorPickServiceAdapter : IColorPickService
    {
        private readonly ICaptureService _captureService;

        public ColorPickServiceAdapter(ICaptureService captureService)
        {
            _captureService = captureService ?? throw new ArgumentNullException(nameof(captureService));
        }

        public bool IsColorPickerActive
        {
            get
            {
                // リフレクションを使用してIsColorPickerActiveプロパティを取得
                var property = _captureService.GetType().GetProperty("IsColorPickerActive");
                return property?.GetValue(_captureService) as bool? ?? false;
            }
        }

        public async Task<System.Drawing.Color?> CaptureColorFromScreenAsync()
        {
            // リフレクションを使用してCaptureColorFromScreenAsyncメソッドを呼び出し
            var method = _captureService.GetType().GetMethod("CaptureColorFromScreenAsync");
            if (method != null)
            {
                var task = method.Invoke(_captureService, null) as Task<System.Drawing.Color?>;
                return await (task ?? Task.FromResult<System.Drawing.Color?>(null));
            }
            
            // フォールバック: 右クリック位置の色を取得
            return await _captureService.CaptureColorAtRightClickAsync();
        }

        public System.Drawing.Color GetColorAt(int x, int y)
        {
            return _captureService.GetColorAt(new System.Drawing.Point(x, y));
        }

        public System.Drawing.Color GetColorAt(System.Drawing.Point point)
        {
            return _captureService.GetColorAt(point);
        }

        public System.Drawing.Color GetColorAtCurrentMousePosition()
        {
            // リフレクションを使用してGetColorAtCurrentMousePositionメソッドを呼び出し
            var method = _captureService.GetType().GetMethod("GetColorAtCurrentMousePosition");
            if (method != null)
            {
                return method.Invoke(_captureService, null) as System.Drawing.Color? ?? System.Drawing.Color.Black;
            }
            
            // フォールバック
            var position = _captureService.GetCurrentMousePosition();
            return _captureService.GetColorAt(position);
        }

        public string ColorToHex(System.Drawing.Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        public System.Drawing.Color? HexToColor(string hex)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(hex))
                    return null;

                hex = hex.TrimStart('#');
                if (hex.Length != 6)
                    return null;

                var r = Convert.ToByte(hex.Substring(0, 2), 16);
                var g = Convert.ToByte(hex.Substring(2, 2), 16);
                var b = Convert.ToByte(hex.Substring(4, 2), 16);

                return System.Drawing.Color.FromArgb(r, g, b);
            }
            catch
            {
                return null;
            }
        }

        public System.Windows.Media.Color ToMediaColor(System.Drawing.Color drawingColor)
        {
            return System.Windows.Media.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
        }

        public System.Drawing.Color ToDrawingColor(System.Windows.Media.Color mediaColor)
        {
            return System.Drawing.Color.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);
        }

        public void CancelColorPicker()
        {
            // リフレクションを使用してCancelColorPickerメソッドを呼び出し
            var method = _captureService.GetType().GetMethod("CancelColorPicker");
            method?.Invoke(_captureService, null);
        }
    }

    /// <summary>
    /// CaptureServiceからIKeyHelperServiceへのアダプター
    /// </summary>
    public class KeyHelperServiceAdapter : IKeyHelperService
    {
        private readonly ICaptureService _captureService;

        public KeyHelperServiceAdapter(ICaptureService captureService)
        {
            _captureService = captureService ?? throw new ArgumentNullException(nameof(captureService));
        }

        public bool IsKeyHelperActive
        {
            get
            {
                // リフレクションを使用してIsKeyHelperActiveプロパティを取得
                var property = _captureService.GetType().GetProperty("IsKeyHelperActive");
                return property?.GetValue(_captureService) as bool? ?? false;
            }
        }

        public void SendGlobalKey(System.Windows.Input.Key key)
        {
            var method = _captureService.GetType().GetMethod("SendGlobalKey", new[] { typeof(System.Windows.Input.Key) });
            method?.Invoke(_captureService, new object[] { key });
        }

        public void SendGlobalKey(System.Windows.Input.Key key, bool ctrl = false, bool alt = false, bool shift = false)
        {
            var method = _captureService.GetType().GetMethod("SendGlobalKey", new[] { typeof(System.Windows.Input.Key), typeof(bool), typeof(bool), typeof(bool) });
            method?.Invoke(_captureService, new object[] { key, ctrl, alt, shift });
        }

        public void SendKeyToWindow(System.Windows.Input.Key key, string windowTitle = "", string windowClassName = "")
        {
            var method = _captureService.GetType().GetMethod("SendKeyToWindow", new[] { typeof(System.Windows.Input.Key), typeof(string), typeof(string) });
            method?.Invoke(_captureService, new object[] { key, windowTitle, windowClassName });
        }

        public void SendKeyToWindow(System.Windows.Input.Key key, bool ctrl = false, bool alt = false, bool shift = false, string windowTitle = "", string windowClassName = "")
        {
            var method = _captureService.GetType().GetMethod("SendKeyToWindow", new[] { typeof(System.Windows.Input.Key), typeof(bool), typeof(bool), typeof(bool), typeof(string), typeof(string) });
            method?.Invoke(_captureService, new object[] { key, ctrl, alt, shift, windowTitle, windowClassName });
        }

        public async Task SendGlobalKeyAsync(System.Windows.Input.Key key, bool ctrl = false, bool alt = false, bool shift = false)
        {
            var method = _captureService.GetType().GetMethod("SendGlobalKeyAsync");
            if (method != null)
            {
                var task = method.Invoke(_captureService, new object[] { key, ctrl, alt, shift }) as Task;
                if (task != null) await task;
            }
        }

        public async Task SendKeyToWindowAsync(System.Windows.Input.Key key, bool ctrl = false, bool alt = false, bool shift = false, string windowTitle = "", string windowClassName = "")
        {
            var method = _captureService.GetType().GetMethod("SendKeyToWindowAsync");
            if (method != null)
            {
                var task = method.Invoke(_captureService, new object[] { key, ctrl, alt, shift, windowTitle, windowClassName }) as Task;
                if (task != null) await task;
            }
        }

        public async Task SendKeySequenceAsync(IEnumerable<System.Windows.Input.Key> keys, int intervalMs = 100)
        {
            var method = _captureService.GetType().GetMethod("SendKeySequenceAsync");
            if (method != null)
            {
                var task = method.Invoke(_captureService, new object[] { keys, intervalMs }) as Task;
                if (task != null) await task;
            }
        }

        public async Task SendTextAsync(string text, int intervalMs = 50)
        {
            var method = _captureService.GetType().GetMethod("SendTextAsync");
            if (method != null)
            {
                var task = method.Invoke(_captureService, new object[] { text, intervalMs }) as Task;
                if (task != null) await task;
            }
        }

        public async Task SendTextToWindowAsync(string text, string windowTitle = "", string windowClassName = "", int intervalMs = 50)
        {
            var method = _captureService.GetType().GetMethod("SendTextToWindowAsync");
            if (method != null)
            {
                var task = method.Invoke(_captureService, new object[] { text, windowTitle, windowClassName, intervalMs }) as Task;
                if (task != null) await task;
            }
        }

        public void CancelKeyHelper()
        {
            var method = _captureService.GetType().GetMethod("CancelKeyHelper");
            method?.Invoke(_captureService, null);
        }
    }

    /// <summary>
    /// ImageProcessingServiceからIOpenCVHelperServiceへのアダプター
    /// </summary>
    public class OpenCVHelperServiceAdapter : IOpenCVHelperService
    {
        private readonly IImageProcessingService _imageProcessingService;

        public OpenCVHelperServiceAdapter(IImageProcessingService imageProcessingService)
        {
            _imageProcessingService = imageProcessingService ?? throw new ArgumentNullException(nameof(imageProcessingService));
        }

        public bool IsProcessingActive => _imageProcessingService.IsProcessingActive;

        public async Task<OpenCvSharp.Mat?> CaptureScreenAsync()
        {
            return await _imageProcessingService.CaptureScreenAsync();
        }

        public async Task<OpenCvSharp.Mat?> CaptureWindowAsync(string windowTitle, string windowClassName = "")
        {
            return await _imageProcessingService.CaptureWindowAsync(windowTitle, windowClassName);
        }

        public async Task<System.Windows.Point?> SearchImageAsync(string imagePath, double threshold = 0.8, CancellationToken cancellationToken = default)
        {
            return await _imageProcessingService.SearchImageOnScreenAsync(imagePath, threshold, cancellationToken);
        }

        public async Task<System.Windows.Point?> SearchImageInWindowAsync(string imagePath, string windowTitle, string windowClassName = "", double threshold = 0.8, CancellationToken cancellationToken = default)
        {
            return await _imageProcessingService.SearchImageInWindowAsync(imagePath, windowTitle, windowClassName, threshold, cancellationToken);
        }

        public async Task<bool> SaveImageAsync(OpenCvSharp.Mat image, string filePath)
        {
            return await _imageProcessingService.SaveImageAsync(image, filePath);
        }

        public void CancelProcessing()
        {
            _imageProcessingService.CancelProcessing();
        }
    }

    /// <summary>
    /// UniversalCommandItemファクトリーインターフェース
    /// </summary>
    public interface IUniversalCommandItemFactory
    {
        UniversalCommandItem? CreateItem(string itemType);
    }

    /// <summary>
    /// UniversalCommandItemファクトリー実装（DirectCommandRegistry統一版）
    /// </summary>
    public class UniversalCommandItemFactory : IUniversalCommandItemFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UniversalCommandItemFactory> _logger;

        public UniversalCommandItemFactory(IServiceProvider serviceProvider, ILogger<UniversalCommandItemFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public UniversalCommandItem? CreateItem(string itemType)
        {
            try
            {
                _logger.LogDebug("UniversalCommandItemFactory.CreateItem開始: {ItemType}", itemType);

                // DirectCommandRegistryを使用してUniversalCommandItem を作成
                try
                {
                    var universalItem = AutoToolCommandRegistry.CreateUniversalItem(itemType);
                    if (universalItem != null)
                    {
                        _logger.LogDebug("DirectCommandRegistryでUniversalCommandItem作成成功: {ItemType}", itemType);
                        return universalItem;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "DirectCommandRegistryでの作成失敗、UniversalCommandItemで直接作成: {ItemType}", itemType);
                }

                // フォールバック: UniversalCommandItem を直接作成
                var fallbackItem = new UniversalCommandItem
                {
                    ItemType = itemType,
                    IsEnable = true
                };

                _logger.LogDebug("UniversalCommandItem で作成成功: {ItemType}", itemType);
                return fallbackItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UniversalCommandItemFactory.CreateItem 中にエラー発生: {ItemType}", itemType);

                // 緊急フォールバック
                return new UniversalCommandItem
                {
                    ItemType = itemType,
                    IsEnable = true
                };
            }
        }
    }

    /// <summary>
    /// DataContextLocatorインターフェース
    /// </summary>
    public interface IDataContextLocator
    {
        T? GetDataContext<T>() where T : class;
        void SetDataContext<T>(T dataContext) where T : class;
    }

    /// <summary>
    /// DataContextLocator実装
    /// </summary>
    public class DataContextLocator : IDataContextLocator
    {
        private readonly Dictionary<Type, object> _dataContexts = new();

        public T? GetDataContext<T>() where T : class
        {
            _dataContexts.TryGetValue(typeof(T), out var dataContext);
            return dataContext as T;
        }

        public void SetDataContext<T>(T dataContext) where T : class
        {
            _dataContexts[typeof(T)] = dataContext;
        }
    }
}