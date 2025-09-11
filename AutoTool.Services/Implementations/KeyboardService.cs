using System.Runtime.InteropServices;
using AutoTool.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutoTool.Services.Implementations;

/// <summary>
/// キーボード操作サービスの実装
/// </summary>
public class KeyboardService : IKeyboardService
{
    private readonly ILogger<KeyboardService> _logger;

    public KeyboardService(ILogger<KeyboardService> logger)
    {
        _logger = logger;
    }

    public async Task SendKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _logger.LogDebug("Sending key: {Key}", key);
            
            if (TryGetVirtualKeyCode(key, out var vkCode))
            {
                keybd_event(vkCode, 0, 0, 0); // Key down
                keybd_event(vkCode, 0, KEYEVENTF_KEYUP, 0); // Key up
            }
            else
            {
                _logger.LogWarning("Unknown key: {Key}", key);
            }
        }, cancellationToken);
    }

    public async Task SendKeyComboAsync(string[] keys, CancellationToken cancellationToken = default)
    {
        await Task.Run(async () =>
        {
            _logger.LogDebug("Sending key combination: {Keys}", string.Join("+", keys));
            
            var vkCodes = new List<byte>();
            foreach (var key in keys)
            {
                if (TryGetVirtualKeyCode(key, out var vkCode))
                {
                    vkCodes.Add(vkCode);
                }
            }

            // Press all keys down
            foreach (var vkCode in vkCodes)
            {
                keybd_event(vkCode, 0, 0, 0);
                await Task.Delay(10, cancellationToken);
            }

            await Task.Delay(50, cancellationToken);

            // Release all keys up (in reverse order)
            for (int i = vkCodes.Count - 1; i >= 0; i--)
            {
                keybd_event(vkCodes[i], 0, KEYEVENTF_KEYUP, 0);
                await Task.Delay(10, cancellationToken);
            }
        }, cancellationToken);
    }

    public async Task SendTextAsync(string text, CancellationToken cancellationToken = default)
    {
        await Task.Run(async () =>
        {
            _logger.LogDebug("Sending text: {Text}", text);
            
            foreach (char c in text)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                    
                // Use Unicode input for text
                keybd_event(0, 0, KEYEVENTF_UNICODE, (uint)c);
                await Task.Delay(10, cancellationToken);
            }
        }, cancellationToken);
    }

    public async Task SendHotkeyAsync(string hotkey, CancellationToken cancellationToken = default)
    {
        var keys = hotkey.Split('+').Select(k => k.Trim()).ToArray();
        await SendKeyComboAsync(keys, cancellationToken);
    }

    public bool IsKeyPressed(string key)
    {
        if (TryGetVirtualKeyCode(key, out var vkCode))
        {
            return (GetAsyncKeyState(vkCode) & 0x8000) != 0;
        }
        return false;
    }

    private static bool TryGetVirtualKeyCode(string key, out byte vkCode)
    {
        vkCode = 0;
        
        var keyMap = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase)
        {
            { "A", 0x41 }, { "B", 0x42 }, { "C", 0x43 }, { "D", 0x44 }, { "E", 0x45 },
            { "F", 0x46 }, { "G", 0x47 }, { "H", 0x48 }, { "I", 0x49 }, { "J", 0x4A },
            { "K", 0x4B }, { "L", 0x4C }, { "M", 0x4D }, { "N", 0x4E }, { "O", 0x4F },
            { "P", 0x50 }, { "Q", 0x51 }, { "R", 0x52 }, { "S", 0x53 }, { "T", 0x54 },
            { "U", 0x55 }, { "V", 0x56 }, { "W", 0x57 }, { "X", 0x58 }, { "Y", 0x59 },
            { "Z", 0x5A },
            { "0", 0x30 }, { "1", 0x31 }, { "2", 0x32 }, { "3", 0x33 }, { "4", 0x34 },
            { "5", 0x35 }, { "6", 0x36 }, { "7", 0x37 }, { "8", 0x38 }, { "9", 0x39 },
            { "Enter", 0x0D }, { "Space", 0x20 }, { "Tab", 0x09 }, { "Escape", 0x1B },
            { "Shift", 0x10 }, { "Ctrl", 0x11 }, { "Alt", 0x12 },
            { "F1", 0x70 }, { "F2", 0x71 }, { "F3", 0x72 }, { "F4", 0x73 },
            { "F5", 0x74 }, { "F6", 0x75 }, { "F7", 0x76 }, { "F8", 0x77 },
            { "F9", 0x78 }, { "F10", 0x79 }, { "F11", 0x7A }, { "F12", 0x7B }
        };

        return keyMap.TryGetValue(key, out vkCode);
    }

    #region Win32 API

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;

    #endregion
}