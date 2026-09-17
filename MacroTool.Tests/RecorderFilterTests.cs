using MacroTool.Application.Engine;
using MacroTool.Domain;

namespace MacroTool.Tests;

public sealed class RecorderFilterTests
{
    private static MacroEvent Mouse(double t) => new() { T = t, IsKey = false, Up = false, X = 1, Y = 1 };

    private static MacroEvent Key(double t) => new() { T = t, IsKey = true, Vk = 65, Scan = 30, Up = false };

    [Fact]
    public void DropTrailingMouse_RemovesMouseAtOrAfterCutoff()
    {
        var events = new[] { Key(100), Mouse(150), Mouse(700), Mouse(750) };

        var kept = Recorder.DropTrailingMouse(events, cutoffMs: 700);

        Assert.Equal(2, kept.Count);
        Assert.Equal(100, kept[0].T);
        Assert.Equal(150, kept[1].T);
    }

    [Fact]
    public void DropTrailingMouse_KeepsKeysInWindow()
    {
        var events = new[] { Mouse(720), Key(730), Mouse(740) };

        var kept = Recorder.DropTrailingMouse(events, cutoffMs: 700);

        Assert.Single(kept);
        Assert.True(kept[0].IsKey);
    }

    [Fact]
    public void DropTrailingMouse_ExactCutoff_IsDropped()
    {
        var events = new[] { Mouse(700) };

        Assert.Empty(Recorder.DropTrailingMouse(events, cutoffMs: 700));
    }

    [Fact]
    public void DropTrailingMouse_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(Recorder.DropTrailingMouse([], cutoffMs: 0));
    }
}
