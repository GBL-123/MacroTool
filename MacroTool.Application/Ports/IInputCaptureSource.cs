namespace MacroTool.Application.Ports;

public interface IInputCaptureSource : IDisposable
{
    event Action<int, int, bool, bool>? KeyCaptured;

    event Action<bool, int, int>? MouseCaptured;

    void Start();

    void Stop();
}
