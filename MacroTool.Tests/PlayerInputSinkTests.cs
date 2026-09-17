using MacroTool.Application.Engine;
using MacroTool.Domain;

namespace MacroTool.Tests;

public sealed class PlayerInputSinkTests
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

    [Fact]
    public void Playback_RunsRoundsThroughSink()
    {
        var sink = new FakeInputSink();
        var rounds = new List<int>();

        var player = new Player(SampleTimeline(), new EngineSettings(50, 0), sink);

        player.RoundCompleted += r => rounds.Add(r);
        player.Start();

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (player.Round < 3 && DateTime.UtcNow < deadline)
            Thread.Sleep(10);
        player.Stop();

        Assert.True(rounds.Count >= 3, $"rounds={rounds.Count}");
        Assert.Contains((30, false, false), sink.KeyCalls);
        Assert.Contains((30, true, false), sink.KeyCalls);
        Assert.Contains((10, 20), sink.MouseDownCalls);
        Assert.True(sink.MouseUpCalls >= 3);
    }

    [Fact]
    public void Playback_StopsWithNoRounds_WhenTimelineEmpty()
    {
        var player = new Player(new MacroTimeline(), new EngineSettings(1, 0), new FakeInputSink());

        player.Start();
        Thread.Sleep(120);
        player.Stop();

        Assert.Equal(0, player.Round);
    }

    [Fact]
    public void Stop_ReleasesHangingKeyDownAndMouseDown()
    {
        var sink = new FakeInputSink();

        var timeline = new MacroTimeline
        {
            DurationMs = 10,
            Events =
            [
                new MacroEvent { T = 0, IsKey = true, Vk = 65, Scan = 30, Up = false },
                new MacroEvent { T = 1, IsKey = false, Up = false, X = 1, Y = 1 }
            ]
        };

        var player = new Player(timeline, new EngineSettings(10, 0), sink);

        player.Start();
        Thread.Sleep(150);
        player.Stop();

        Assert.Contains((30, true, false), sink.KeyCalls);
        Assert.True(sink.MouseUpCalls >= 1);
        // 释放必须是最后发出的
        var lastKey = sink.KeyCalls[^1];
        Assert.True(lastKey.Up);
    }

    [Fact]
    public void Playback_ReportsInjectionFailure()
    {
        var sink = new FakeInputSink { Result = false };
        var failures = 0;
        var player = new Player(SampleTimeline(), new EngineSettings(50, 0), sink);
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
        var sink = new FakeInputSink();
        var rounds = new List<int>();
        var completedRounds = new List<int>();

        var player = new Player(SampleTimeline(), new EngineSettings(50, 0, repeatCount: 2), sink);
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
        // 自然完成后回放循环已退出，注入不再被调用
        var settledKeyCalls = sink.KeyCalls.Count;
        Thread.Sleep(100);
        Assert.Equal(settledKeyCalls, sink.KeyCalls.Count);
    }
}
