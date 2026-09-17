using MacroTool.Application.Ports;

namespace MacroTool.Tests;

internal sealed class FakeGlobalHotkeys : IGlobalHotkeys
{
    public Action? OnF10 { get; set; }

    public Action? OnF11 { get; set; }

    public Action? OnF12 { get; set; }

    public HotkeyStatus Status { get; private set; } = new(false, false, "F12", true);

    public event Action? StatusChanged;

    public bool Started { get; private set; }

    public void Start() => Started = true;

    public void Dispose()
    {
    }

    public void Report(HotkeyStatus status)
    {
        Status = status;
        StatusChanged?.Invoke();
    }
}
