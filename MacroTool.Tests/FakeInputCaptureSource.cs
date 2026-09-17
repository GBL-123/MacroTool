using MacroTool.Application.Ports;

namespace MacroTool.Tests;

internal sealed class FakeInputCaptureSource : IInputCaptureSource
{
    public event Action<int, int, bool, bool>? KeyCaptured;

    public event Action<bool, int, int>? MouseCaptured;

    public bool Started { get; private set; }

    public void Start() => Started = true;

    public void Stop() => Started = false;

    public void Dispose()
    {
    }

    public void RaiseKey(int vk, int scan, bool ext, bool up) => KeyCaptured?.Invoke(vk, scan, ext, up);

    public void RaiseMouse(bool isDown, int x, int y) => MouseCaptured?.Invoke(isDown, x, y);
}
