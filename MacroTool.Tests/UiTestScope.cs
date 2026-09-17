using Bunit;
using MacroTool.Application.Engine;
using MacroTool.Application.Ports;
using MacroTool.Domain;
using MacroTool.Infrastructure.Hotkeys;
using MacroTool.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace MacroTool.Tests;

/// <summary>Shared bUnit scope: real engine collaborators pointed at a temp dir, no native hooks started.</summary>
public sealed class UiTestScope : IDisposable
{
    public BunitContext Context { get; }

    public MacroEngine Engine { get; }

    public TimelineStore Store { get; }

    public EngineSettings Settings { get; }

    public LogBuffer Logs { get; } = new();

    public HotkeyHost Hotkeys { get; } = new();

    public string TempDir { get; }

    public UiTestScope(IInputSink? sink = null)
    {
        TempDir = Path.Combine(Path.GetTempPath(), "MacroToolUiTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(TempDir);

        Settings = new EngineSettings(1.0, 2);
        Store = new TimelineStore(Path.Combine(TempDir, "timeline.json"));
        Engine = new MacroEngine(Hotkeys, Store, Settings, Logs, NullLogger<MacroEngine>.Instance, sink ?? new FakeInputSink(), () => new FakeInputCaptureSource(), isElevated: false);

        Context = new BunitContext();
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        Context.Services.AddMudServices();
        Context.Services.AddSingleton(Engine);
        Context.Services.AddSingleton(Store);
        Context.Services.AddSingleton(Settings);
        Context.Services.AddSingleton(Logs);
        Context.Services.AddSingleton(Hotkeys);
    }

    public static MacroEvent Click(double t, bool up, int x = 100, int y = 200)
        => new() { T = t, IsKey = false, Up = up, X = x, Y = y };

    public static MacroEvent Key(double t, bool up, int vk = 65, int scan = 30)
        => new() { T = t, IsKey = true, Vk = vk, Scan = scan, Up = up };

    public static MacroTimeline SampleTimeline() => new()
    {
        DurationMs = 500,
        Events =
        [
            Click(100, up: false),
            Click(180, up: true),
            Key(300, up: false),
            Key(360, up: true),
            Key(500, up: false, vk: 66, scan: 48)
        ]
    };

    public void Dispose()
    {
        Context.DisposeAsync().AsTask().GetAwaiter().GetResult();
        Engine.Dispose();
        try
        {
            Directory.Delete(TempDir, recursive: true);
        }
        catch
        {
        }
    }
}
