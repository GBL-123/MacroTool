using MacroTool.Application;
using MacroTool.Application.Engine;
using MacroTool.Application.Hosting;
using MacroTool.Application.Hotkeys;
using MacroTool.Application.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace MacroTool.Tests;

/// <summary>
/// Integration smoke covering the native wiring on Windows: engine start
/// (hotkey thread), hosted service lifecycle, and recorder hook start/stop.
/// Briefly registers global hotkeys F10/F11/F12, then unregisters them.
/// </summary>
public sealed class EngineHostingIntegrationTests
{
    private static string NewTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "MacroToolHost_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public async Task HostedService_StartAndStop_BootsHotkeysAndLogsStatus()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var ct = Xunit.TestContext.Current.CancellationToken;
        var dir = NewTempDir();
        var logs = new LogBuffer();
        var engine = new MacroEngine(
            new HotkeyHost(),
            new TimelineStore(Path.Combine(dir, "timeline.json")),
            new EngineSettings(1.0, 2),
            logs,
            NullLogger<MacroEngine>.Instance);
        var options = Options.Create(new MacroOptions { Speed = -1, Jitter = -5 });
        var lifetime = new Mock<IHostApplicationLifetime>();

        var service = new MacroHostedService(
            engine,
            lifetime.Object,
            options,
            logs,
            NullLogger<MacroHostedService>.Instance);
        try
        {
            await service.StartAsync(ct);

            Assert.Equal(ToolState.Idle, engine.State);
            var entries = logs.Snapshot();
            Assert.Contains(entries, e => e.Message.Contains("Macro:Speed"));

            // 热键状态由专用线程异步上报，等待其到达日志缓冲
            var deadline = DateTime.UtcNow.AddSeconds(3);
            while (!entries.Any(e => e.Message.Contains("热键") || e.Message.Contains("退出"))
                   && DateTime.UtcNow < deadline)
            {
                await Task.Delay(50, ct);
                entries = logs.Snapshot();
            }
            Assert.Contains(entries, e => e.Message.Contains("热键") || e.Message.Contains("退出"));
        }
        finally
        {
            await service.StopAsync(ct);
            engine.Dispose();
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Recorder_StartStopWithoutInput_ProducesEmptyTimeline()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var ct = Xunit.TestContext.Current.CancellationToken;
        var recorder = new Recorder();
        recorder.Start();
        await Task.Delay(120, ct);
        var timeline = recorder.Stop(dropTrailingMouse: true);

        Assert.Empty(timeline.Events);
        Assert.Equal(0, recorder.ClickCount);
        Assert.Equal(0, recorder.KeyCount);
    }

    [Fact]
    public async Task BrowserLauncher_StartAsync_Disabled_DoesNotThrow()
    {
        var ct = Xunit.TestContext.Current.CancellationToken;
        var launcher = new BrowserLauncher(
            Options.Create(new MacroOptions { OpenBrowser = false }),
            server: null!,
            lifetime: new Mock<IHostApplicationLifetime>().Object,
            logger: NullLogger<BrowserLauncher>.Instance);

        await launcher.StartAsync(ct);
        await launcher.StopAsync(ct);
    }
}
