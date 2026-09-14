using MacroTool.Application.Interop;
using MacroTool.Application.Engine;

namespace MacroTool.Tests;

public sealed class RecorderProcessTests
{
    private static Recorder NewRecorder() => new();

    [Fact]
    public void ProcessKey_CapturesDownUpPair()
    {
        var recorder = NewRecorder();

        recorder.ProcessKey(65, 30, ext: false, up: false);
        recorder.ProcessKey(65, 30, ext: false, up: true);

        var timeline = recorder.Stop(dropTrailingMouse: false);
        Assert.Equal(2, timeline.Events.Count);
        Assert.False(timeline.Events[0].Up);
        Assert.True(timeline.Events[1].Up);
        Assert.Equal(65, timeline.Events[0].Vk);
        Assert.Equal(30, timeline.Events[0].Scan);
        Assert.Equal(1, recorder.KeyCount);
    }

    [Fact]
    public void ProcessKey_DeduplicatesRepeatDowns()
    {
        var recorder = NewRecorder();

        recorder.ProcessKey(65, 30, false, false);
        recorder.ProcessKey(65, 30, false, false);
        recorder.ProcessKey(65, 30, false, true);
        recorder.ProcessKey(65, 30, false, true);

        var timeline = recorder.Stop(false);
        Assert.Equal(2, timeline.Events.Count);
        Assert.Equal(1, recorder.KeyCount);
    }

    [Fact]
    public void ProcessKey_ExcludesToolHotkeys()
    {
        var recorder = NewRecorder();

        recorder.ProcessKey(Native.VK_F10, 0x44, false, false);
        recorder.ProcessKey(Native.VK_F10, 0x44, false, true);
        recorder.ProcessKey(Native.VK_F11, 0x45, false, false);
        recorder.ProcessKey(Native.VK_F11, 0x45, false, true);
        recorder.ProcessKey(Native.VK_F12, 0x46, false, false);
        recorder.ProcessKey(Native.VK_F12, 0x46, false, true);

        var timeline = recorder.Stop(false);
        Assert.Empty(timeline.Events);
        Assert.Equal(0, recorder.KeyCount);
    }

    [Fact]
    public void ProcessMouse_IgnoresNonLeftButtonMessages()
    {
        var recorder = NewRecorder();

        recorder.ProcessMouse(0x0204, 1, 1);
        recorder.ProcessMouse(0x0207, 1, 1);

        var timeline = recorder.Stop(false);
        Assert.Empty(timeline.Events);
        Assert.Equal(0, recorder.ClickCount);
    }

    [Fact]
    public void ProcessMouse_CapturesClickPairWithCoordinates()
    {
        var recorder = NewRecorder();

        recorder.ProcessMouse(Native.WM_LBUTTONDOWN, 100, 200);
        recorder.ProcessMouse(Native.WM_LBUTTONDOWN, 100, 200);
        recorder.ProcessMouse(Native.WM_LBUTTONUP, 100, 200);
        recorder.ProcessMouse(Native.WM_LBUTTONUP, 100, 200);

        var timeline = recorder.Stop(false);
        Assert.Equal(2, timeline.Events.Count);
        Assert.False(timeline.Events[0].Up);
        Assert.Equal(100, timeline.Events[0].X);
        Assert.Equal(200, timeline.Events[0].Y);
        Assert.True(timeline.Events[1].Up);
        Assert.Equal(1, recorder.ClickCount);
    }

    [Fact]
    public void Stop_DropTrailingMouse_FiltersTailMouseOnly()
    {
        var recorder = NewRecorder();

        recorder.ProcessKey(65, 30, false, false);
        recorder.ProcessMouse(Native.WM_LBUTTONDOWN, 5, 5);
        recorder.ProcessMouse(Native.WM_LBUTTONUP, 5, 5);

        var timeline = recorder.Stop(dropTrailingMouse: true);

        Assert.Single(timeline.Events);
        Assert.True(timeline.Events[0].IsKey);
    }
}
