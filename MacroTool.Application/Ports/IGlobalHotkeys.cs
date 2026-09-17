namespace MacroTool.Application.Ports;

public sealed record HotkeyStatus(
    bool F10Registered,
    bool F11Registered,
    string ExitKeyLabel,
    bool ExitKeyAvailable)
{
    public bool FullyOperational => F10Registered && F11Registered;
}

public interface IGlobalHotkeys : IDisposable
{
    Action? OnF10 { get; set; }

    Action? OnF11 { get; set; }

    Action? OnF12 { get; set; }

    HotkeyStatus Status { get; }

    event Action? StatusChanged;

    void Start();
}
