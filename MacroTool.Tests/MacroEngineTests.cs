using MacroTool.Application.Engine;
using MacroTool.Application.Hotkeys;
using MacroTool.Application.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace MacroTool.Tests;

public sealed class MacroEngineTests : IDisposable
{
    private readonly string _dir;
    private readonly MacroEngine _engine;

    public MacroEngineTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "MacroToolEngineTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _engine = NewEngine(delegates: null);
    }

    private MacroEngine NewEngine(PlayerDelegates? delegates)
        => new(
            new HotkeyHost(),
            new TimelineStore(Path.Combine(_dir, "timeline.json")),
            new EngineSettings(1.0, 2),
            new LogBuffer(),
            NullLogger<MacroEngine>.Instance,
            delegates);

    public void Dispose()
    {
        _engine.Dispose();
        try
        {
            Directory.Delete(_dir, recursive: true);
        }
        catch
        {
        }
    }

    private static MacroTimeline SampleTimeline(int duration = 200) => new()
    {
        DurationMs = duration,
        Events = [new MacroEvent { T = 100, IsKey = false, Up = false, X = 1, Y = 1 }]
    };

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void UpdateSettings_InvalidSpeed_IsRejected(double speed)
    {
        Assert.False(_engine.UpdateSettings(speed, 2, 0, out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void UpdateSettings_InvalidJitter_IsRejected()
    {
        Assert.False(_engine.UpdateSettings(1.5, -1, 0, out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void UpdateSettings_ValidValues_AreApplied()
    {
        Assert.True(_engine.UpdateSettings(2.5, 5, 3, out var error));
        Assert.Null(error);
    }

    [Fact]
    public void UpdateSettings_NegativeRepeatCount_IsRejected()
    {
        Assert.False(_engine.UpdateSettings(1.5, 2, -1, out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public async Task StartPlayback_WithRepeatCount_CompletesNaturally()
    {
        var engine = new MacroEngine(
            new HotkeyHost(),
            new TimelineStore(Path.Combine(_dir, "timeline.json")),
            new EngineSettings(10, 0, repeatCount: 3),
            new LogBuffer(),
            NullLogger<MacroEngine>.Instance,
            new PlayerDelegates((_, _, _) => true, (_, _) => true, () => true));
        try
        {
            engine.ReplaceTimeline(SampleTimeline(duration: 20));
            engine.StartPlayback();
            Assert.Equal(ToolState.Playing, engine.State);

            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (engine.State == ToolState.Playing && DateTime.UtcNow < deadline)
                await Task.Delay(50);

            Assert.Equal(ToolState.Idle, engine.State);
            Assert.Equal(3, engine.GetSnapshot().PlaybackRound);
        }
        finally
        {
            engine.Dispose();
        }
    }

    [Fact]
    public void ReplaceTimeline_WhenIdle_UpdatesCurrentTimeline()
    {
        var raised = false;
        _engine.TimelineChanged += () => raised = true;

        var timeline = SampleTimeline();
        _engine.ReplaceTimeline(timeline);

        Assert.Same(timeline, _engine.CurrentTimeline);
        Assert.True(raised);
    }

    [Fact]
    public void ReplaceTimeline_WhilePlaying_IsIgnored()
    {
        var playback = NewEngine(new PlayerDelegates(
            (_, _, _) => true, (_, _) => true, () => true));
        playback.ReplaceTimeline(SampleTimeline());
        playback.StartPlayback();
        try
        {
            Assert.Equal(ToolState.Playing, playback.State);

            var replacement = SampleTimeline(999);
            playback.ReplaceTimeline(replacement);

            Assert.NotSame(replacement, playback.CurrentTimeline);
        }
        finally
        {
            playback.StopPlayback();
            playback.Dispose();
        }
    }

    [Fact]
    public void StartPlayback_WithInjectedDelegates_EntersPlayingAndStops()
    {
        var engine = NewEngine(new PlayerDelegates(
            (_, _, _) => true, (_, _) => true, () => true));
        engine.ReplaceTimeline(SampleTimeline(duration: 20));

        engine.StartPlayback();
        Assert.Equal(ToolState.Playing, engine.State);

        engine.StopPlayback();
        Assert.Equal(ToolState.Idle, engine.State);
        engine.Dispose();
    }

    [Fact]
    public void StartPlayback_WithoutTimeline_IsRejected()
    {
        _engine.StartPlayback();

        Assert.Equal(ToolState.Idle, _engine.State);
    }

    [Fact]
    public void TogglePlayback_TogglesState()
    {
        var engine = NewEngine(new PlayerDelegates(
            (_, _, _) => true, (_, _) => true, () => true));
        engine.ReplaceTimeline(SampleTimeline(duration: 20));

        engine.TogglePlayback();
        Assert.Equal(ToolState.Playing, engine.State);
        engine.TogglePlayback();
        Assert.Equal(ToolState.Idle, engine.State);
        engine.Dispose();
    }

    [Fact]
    public void RequestShutdown_RaisesShutdownRequested()
    {
        var raised = false;
        _engine.ShutdownRequested += () => raised = true;

        _engine.RequestShutdown(fromUi: true);

        Assert.True(raised);
        Assert.Equal(ToolState.Idle, _engine.State);
    }

    [Fact]
    public void GetSnapshot_ReflectsCurrentTimeline()
    {
        var engine = NewEngine(new PlayerDelegates(
            (_, _, _) => true, (_, _) => true, () => true));
        var timeline = new MacroTimeline
        {
            DurationMs = 500,
            Events =
            [
                new MacroEvent { T = 100, IsKey = false, Up = false, X = 1, Y = 1 },
                new MacroEvent { T = 400, IsKey = false, Up = true }
            ]
        };
        engine.ReplaceTimeline(timeline);

        var snapshot = engine.GetSnapshot();

        Assert.Equal(ToolState.Idle, snapshot.State);
        Assert.Equal(2, snapshot.EventCount);
        Assert.Equal(500, snapshot.DurationMs);
        Assert.True(snapshot.HasTimeline);
        engine.Dispose();
    }
}
