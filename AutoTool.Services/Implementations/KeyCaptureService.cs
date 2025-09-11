using AutoTool.Services.Abstractions;
using AutoTool.Services.ColorPicking;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace AutoTool.Services.Implementations;

/// <summary>
/// キーキャプチャ専用サービス
/// </summary>
public class KeyCaptureService : IKeyCaptureService
{
    private readonly ILogger<KeyCaptureService> _logger;
    private readonly IKeyboardService _keyboardService;
    private bool _isCapturing;

    public KeyCaptureService(
        ILogger<KeyCaptureService> logger,
        IKeyboardService keyboardService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keyboardService = keyboardService ?? throw new ArgumentNullException(nameof(keyboardService));
    }

    public bool IsKeyHelperActive => _isCapturing;

    public async Task<KeyCaptureResult?> CaptureKeyAsync(string title)
    {
        _logger.LogDebug("Starting key capture with title: {Title}", title);
        
        _isCapturing = true;
        
        try
        {
            return await WaitForKeyPressAsync();
        }
        finally
        {
            _isCapturing = false;
        }
    }

    public void CancelKeyCapture()
    {
        _isCapturing = false;
        _logger.LogDebug("Key capture cancelled");
    }

    private async Task<KeyCaptureResult?> WaitForKeyPressAsync()
    {
        var pressedKeys = new HashSet<int>();
        
        while (_isCapturing)
        {
            if (IsKeyPressed(VK_ESCAPE))
            {
                _logger.LogDebug("Key capture cancelled by ESC");
                return null;
            }

            // 主要なキーをチェック
            for (int vk = 8; vk <= 255; vk++)
            {
                if (IsKeyPressed(vk) && !pressedKeys.Contains(vk))
                {
                    pressedKeys.Add(vk);
                    
                    var result = CreateKeyCaptureResult(vk);
                    if (result != null)
                    {
                        _logger.LogDebug("Key captured: {Key}", result.Key);
                        return result;
                    }
                }
                else if (!IsKeyPressed(vk) && pressedKeys.Contains(vk))
                {
                    pressedKeys.Remove(vk);
                }
            }

            await Task.Delay(50);
        }

        return null;
    }

    private KeyCaptureResult? CreateKeyCaptureResult(int virtualKey)
    {
        var keyName = GetKeyName(virtualKey);
        if (string.IsNullOrEmpty(keyName)) 
            return null;

        return new KeyCaptureResult
        {
            Key = keyName,
            Ctrl = IsKeyPressed(VK_CONTROL),
            Alt = IsKeyPressed(VK_MENU),
            Shift = IsKeyPressed(VK_SHIFT),
            CapturedAt = DateTime.Now
        };
    }

    private string? GetKeyName(int virtualKey)
    {
        return virtualKey switch
        {
            VK_ESCAPE => "Escape",
            VK_SPACE => "Space",
            VK_RETURN => "Return",
            VK_TAB => "Tab",
            VK_BACK => "Backspace",
            VK_DELETE => "Delete",
            VK_INSERT => "Insert",
            VK_HOME => "Home",
            VK_END => "End",
            VK_PRIOR => "PageUp",
            VK_NEXT => "PageDown",
            VK_UP => "Up",
            VK_DOWN => "Down",
            VK_LEFT => "Left",
            VK_RIGHT => "Right",
            VK_F1 => "F1",
            VK_F2 => "F2",
            VK_F3 => "F3",
            VK_F4 => "F4",
            VK_F5 => "F5",
            VK_F6 => "F6",
            VK_F7 => "F7",
            VK_F8 => "F8",
            VK_F9 => "F9",
            VK_F10 => "F10",
            VK_F11 => "F11",
            VK_F12 => "F12",
            >= 0x30 and <= 0x39 => ((char)virtualKey).ToString(), // 0-9
            >= 0x41 and <= 0x5A => ((char)virtualKey).ToString(), // A-Z
            _ => null
        };
    }

    #region Win32 API

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private bool IsKeyPressed(int vKey)
    {
        return (GetAsyncKeyState(vKey) & 0x8000) != 0;
    }

    // Virtual Key Codes
    private const int VK_ESCAPE = 0x1B;
    private const int VK_SPACE = 0x20;
    private const int VK_RETURN = 0x0D;
    private const int VK_TAB = 0x09;
    private const int VK_BACK = 0x08;
    private const int VK_DELETE = 0x2E;
    private const int VK_INSERT = 0x2D;
    private const int VK_HOME = 0x24;
    private const int VK_END = 0x23;
    private const int VK_PRIOR = 0x21;
    private const int VK_NEXT = 0x22;
    private const int VK_UP = 0x26;
    private const int VK_DOWN = 0x28;
    private const int VK_LEFT = 0x25;
    private const int VK_RIGHT = 0x27;
    private const int VK_F1 = 0x70;
    private const int VK_F2 = 0x71;
    private const int VK_F3 = 0x72;
    private const int VK_F4 = 0x73;
    private const int VK_F5 = 0x74;
    private const int VK_F6 = 0x75;
    private const int VK_F7 = 0x76;
    private const int VK_F8 = 0x77;
    private const int VK_F9 = 0x78;
    private const int VK_F10 = 0x79;
    private const int VK_F11 = 0x7A;
    private const int VK_F12 = 0x7B;
    private const int VK_SHIFT = 0x10;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12; // Alt

    #endregion
}