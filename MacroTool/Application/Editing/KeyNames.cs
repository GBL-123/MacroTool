namespace MacroTool.Application.Editing;

public static class KeyNames
{
    public static string Format(int vk) => vk switch
    {
        >= 0x41 and <= 0x5A => ((char)vk).ToString(),
        >= 0x30 and <= 0x39 => ((char)vk).ToString(),
        >= 0x70 and <= 0x7B => "F" + (vk - 0x6F),
        0x20 => "Space",
        0x0D => "Enter",
        0x1B => "Esc",
        0x09 => "Tab",
        0x08 => "Backspace",
        0x10 => "Shift",
        0x11 => "Ctrl",
        0x12 => "Alt",
        0x25 => "Left",
        0x26 => "Up",
        0x27 => "Right",
        0x28 => "Down",
        0x2E => "Delete",
        0x24 => "Home",
        0x23 => "End",
        0x21 => "PageUp",
        0x22 => "PageDown",
        0x14 => "CapsLock",
        0x5B => "Win",
        0x0C => "Clear",
        0x0A => "LineFeed",
        _ => $"VK 0x{vk:X2}"
    };
}
