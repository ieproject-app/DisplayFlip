using System;
using System.Collections.Generic;

namespace DisplayFlip.UI.Helpers;

/// <summary>
/// Parser dan konverter representasi pintasan keyboard ke Win32 modifiers & Virtual-Key codes.
/// </summary>
public static class HotkeyInterop
{
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;

    // VK constants
    public const uint VK_BACK = 0x08;
    public const uint VK_TAB = 0x09;
    public const uint VK_RETURN = 0x0D;
    public const uint VK_ESCAPE = 0x1B;
    public const uint VK_SPACE = 0x20;
    public const uint VK_PRIOR = 0x21; // PageUp
    public const uint VK_NEXT = 0x22;  // PageDown
    public const uint VK_END = 0x23;
    public const uint VK_HOME = 0x24;
    public const uint VK_LEFT = 0x25;
    public const uint VK_UP = 0x26;
    public const uint VK_RIGHT = 0x27;
    public const uint VK_DOWN = 0x28;
    public const uint VK_INSERT = 0x2D;
    public const uint VK_DELETE = 0x2E;
    public const uint VK_F1 = 0x70;
    public const uint VK_F24 = 0x87;
    public const uint VK_NUMPAD0 = 0x60;
    public const uint VK_NUMPAD9 = 0x69;

    /// <summary>
    /// Mem-parsing string shortcut (misal "Ctrl+Alt+F", "F9", "Ctrl+Shift+1")
    /// menjadi kombinasi flag Win32 modifiers dan VK code.
    /// </summary>
    public static bool TryParse(string? shortcut, out uint modifiers, out uint vk)
    {
        modifiers = 0;
        vk = 0;

        if (string.IsNullOrWhiteSpace(shortcut))
            return false;

        var tokens = shortcut.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
            return false;

        uint parsedKey = 0;

        foreach (var raw in tokens)
        {
            var token = raw.Trim();
            if (token.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || token.Equals("Control", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= MOD_CONTROL;
                continue;
            }
            if (token.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= MOD_ALT;
                continue;
            }
            if (token.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= MOD_SHIFT;
                continue;
            }
            if (token.Equals("Win", StringComparison.OrdinalIgnoreCase) || token.Equals("Windows", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= MOD_WIN;
                continue;
            }

            // Jika sudah ada tombol utama sebelumnya, kombinasi multi-key tidak valid
            if (parsedKey != 0)
                return false;

            // 1. Angka 0-9
            if (token.Length == 1 && char.IsDigit(token[0]))
            {
                parsedKey = (uint)('0' + (token[0] - '0'));
            }
            // 2. Huruf A-Z
            else if (token.Length == 1 && char.IsLetter(token[0]))
            {
                parsedKey = (uint)char.ToUpperInvariant(token[0]);
            }
            // 3. Numpad: Num0 - Num9 atau NumPad0 - NumPad9
            else if (token.StartsWith("numpad", StringComparison.OrdinalIgnoreCase) && token.Length == 7 && char.IsDigit(token[6]))
            {
                parsedKey = VK_NUMPAD0 + (uint)(token[6] - '0');
            }
            else if (token.StartsWith("num", StringComparison.OrdinalIgnoreCase) && token.Length == 4 && char.IsDigit(token[3]))
            {
                parsedKey = VK_NUMPAD0 + (uint)(token[3] - '0');
            }
            // 4. Tombol fungsi F1 - F24
            else if (token.Length >= 2 && (token[0] == 'f' || token[0] == 'F') && int.TryParse(token.Substring(1), out int fNum) && fNum is >= 1 and <= 24)
            {
                parsedKey = VK_F1 + (uint)(fNum - 1);
            }
            // 5. Tombol arah & navigasi
            else if (token.Equals("left", StringComparison.OrdinalIgnoreCase)) parsedKey = VK_LEFT;
            else if (token.Equals("right", StringComparison.OrdinalIgnoreCase)) parsedKey = VK_RIGHT;
            else if (token.Equals("up", StringComparison.OrdinalIgnoreCase)) parsedKey = VK_UP;
            else if (token.Equals("down", StringComparison.OrdinalIgnoreCase)) parsedKey = VK_DOWN;
            else if (token.Equals("home", StringComparison.OrdinalIgnoreCase)) parsedKey = VK_HOME;
            else if (token.Equals("end", StringComparison.OrdinalIgnoreCase)) parsedKey = VK_END;
            else if (token.Equals("pageup", StringComparison.OrdinalIgnoreCase) || token.Equals("prior", StringComparison.OrdinalIgnoreCase)) parsedKey = VK_PRIOR;
            else if (token.Equals("pagedown", StringComparison.OrdinalIgnoreCase) || token.Equals("next", StringComparison.OrdinalIgnoreCase)) parsedKey = VK_NEXT;
            else if (token.Equals("insert", StringComparison.OrdinalIgnoreCase)) parsedKey = VK_INSERT;
            else if (token.Equals("delete", StringComparison.OrdinalIgnoreCase) || token.Equals("del", StringComparison.OrdinalIgnoreCase)) parsedKey = VK_DELETE;
            else if (token.Equals("back", StringComparison.OrdinalIgnoreCase) || token.Equals("backspace", StringComparison.OrdinalIgnoreCase)) parsedKey = VK_BACK;
            else if (token.Equals("space", StringComparison.OrdinalIgnoreCase)) parsedKey = VK_SPACE;
            else if (token.Equals("tab", StringComparison.OrdinalIgnoreCase)) parsedKey = VK_TAB;
            else if (token.Equals("enter", StringComparison.OrdinalIgnoreCase) || token.Equals("return", StringComparison.OrdinalIgnoreCase)) parsedKey = VK_RETURN;
            else if (token.Equals("esc", StringComparison.OrdinalIgnoreCase) || token.Equals("escape", StringComparison.OrdinalIgnoreCase)) parsedKey = VK_ESCAPE;
            else
            {
                return false;
            }
        }

        if (parsedKey == 0)
            return false;

        // Validasi: tombol non-F1-F24 wajib memiliki modifier
        bool isFunctionKey = parsedKey is >= VK_F1 and <= VK_F24;
        if (modifiers == 0 && !isFunctionKey)
            return false;

        vk = parsedKey;
        return true;
    }
}
