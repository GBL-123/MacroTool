using MacroTool.Application.Engine;

namespace MacroTool.Tests;

public sealed class PlayerDelegatesTests
{
    private static MacroTimeline SampleTimeline() => new()
    {
        DurationMs = 12,
        Events =
        [
            new MacroEvent { T = 1, IsKey = true, Vk = 65, Scan = 30, Up = false },
            new MacroEvent { T = 2, IsKey = true, Vk = 65, Scan = 30, Up = true },
            new MacroEvent { T = 3, IsKey = false, Up = false, X = 10, Y = 20 },
            new MacroEvent { T = 4, IsKey = false, Up = true }
        ]
    };

    private static PlayerDelegates Recording(Action<int, bool, bool> key,
        Action<int, int> mouseDown, Action mouseUp, bool result = true)
        => new(
            (scan, up, ext) => { key(scan, up, ext); return result; },
            (x, y) => { mouseDown(x, y); return result; },
            () => { mouseUp(); return result; });

    [Fact]
    public void Playback_RunsRoundsThroughDelegates()
    {
        var keyCalls = new List<(int Scan, bool Up)>();
        var mouseDownCalls = new List<(int X, int Y)>();
        var mouseUpCalls = 0;
        var rounds = new List<int>();

        var player = new Player(SampleTimeline(), new EngineSettings(50, 0), Recording(
            (scan, up, ext) => keyCalls.Add((scan, up)),
            (x, y) => mouseDownCalls.Add((x, y)),
            () => mouseUpCalls++));

        player.RoundCompleted += r => rounds.Add(r);
        player.Start();

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (player.Round < 3 && DateTime.UtcNow < deadline)
            Thread.Sleep(10);
        player.Stop();

        Assert.True(rounds.Count >= 3, $"rounds={rounds.Count}");
        Assert.Contains((30, false), keyCalls);
        Assert.Contains((30, true), keyCalls);
        Assert.Contains((10, 20), mouseDownCalls);
        Assert.True(mouseUpCalls >= 3);
    }

    [Fact]
    public void Playback_StopsWithNoRounds_WhenTimelineEmpty()
    {
        var player = new Player(new MacroTimeline(), new EngineSettings(1, 0), Recording(
            (_, _, _) => { }, (_, _) => { }, () => { }));

        player.Start();
        Thread.Sleep(120);
        player.Stop();

        Assert.Equal(0, player.Round);
    }

    [Fact]
    public void Stop_ReleasesHangingKeyDownAndMouseDown()
    {
        var keyCalls = new List<(int Scan, bool Up, bool Ext)>();
        var mouseUpCalls = 0;

        var timeline = new MacroTimeline
        {
            DurationMs = 10,
            Events =
            [
                new MacroEvent { T = 0, IsKey = true, Vk = 65, Scan = 30, Up = false },
                new MacroEvent { T = 1, IsKey = false, Up = false, X = 1, Y = 1 }
            ]
        };

        var player = new Player(timeline, new EngineSettings(10, 0), Recording(
            (scan, up, ext) => keyCalls.Add((scan, up, ext)),
            (_, _) => { },
            () => mouseUpCalls++));

        player.Start();
        Thread.Sleep(150);
        player.Stop();

        Assert.Contains((30, true, false), keyCalls);
        Assert.True(mouseUpCalls >= 1);
        // 閲婃斁蹇呴』鏄渶鍚庡彂鍑虹殑
        var lastKey = keyCalls[^1];
        Assert.True(lastKey.Up);
    }

    [Fact]
    public void Playback_ReportsInjectionFailure()
    {
        var failures = 0;
        var player = new Player(SampleTimeline(), new EngineSettings(50, 0), Recording(
            (_, _, _) => { }, (_, _) => { }, () => { }, result: false));
        player.InjectionFailed += () => failures++;

        player.Start();
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (player.Round < 2 && DateTime.UtcNow < deadline)
            Thread.Sleep(10);
        player.Stop();

        Assert.True(failures >= 2, $"failures={failures}");
    }

    [Fact]
    public void Playback_StopsAfterRepeatCount_AndRaisesCompleted()
    {
        var keyCalls = 0;
        var rounds = new List<int>();
        var completedRounds = new List<int>();

        var player = new Player(SampleTimeline(), new EngineSettings(50, 0, repeatCount: 2), Recording(
            (_, _, _) => keyCalls++,
            (_, _) => { },
            () => { }));
        player.RoundCompleted += r => rounds.Add(r);
        player.Completed += r => completedRounds.Add(r);

        player.Start();
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (completedRounds.Count == 0 && DateTime.UtcNow < deadline)
            Thread.Sleep(20);

        var settledRound = player.Round;
        Thread.Sleep(150);

        Assert.Equal(2, settledRound);
        Assert.Equal(2, rounds.Count);
        Assert.Single(completedRounds);
        Assert.Equal(2, completedRounds[0]);
        // 自然完成后回放循环已退出，委托不再被调用
        var settledKeyCalls = keyCalls;
        Thread.Sleep(100);
        Assert.Equal(settledKeyCalls, keyCalls);
    }
}
