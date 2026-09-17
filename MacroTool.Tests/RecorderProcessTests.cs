using MacroTool.Application.Engine;

namespace MacroTool.Tests;

public sealed class RecorderProcessTests
{
    private readonly FakeInputCaptureSource _source = new();

    private Recorder NewRecorder() => new(_source);

    [Fact]
    public void KeyCapture_CapturesDownUpPair()
    {
        var recorder = NewRecorder();

        _source.RaiseKey(65, 30, ext: false, up: false);
        _source.RaiseKey(65, 30, ext: false, up: true);

        var timeline = recorder.Stop(dropTrailingMouse: false);
        Assert.Equal(2, timeline.Events.Count);
        Assert.False(timeline.Events[0].Up);
        Assert.True(timeline.Events[1].Up);
        Assert.Equal(65, timeline.Events[0].Vk);
        Assert.Equal(30, timeline.Events[0].Scan);
        Assert.Equal(1, recorder.KeyCount);
    }

    [Fact]
    public void KeyCapture_DeduplicatesRepeatDowns()
    {
        var recorder = NewRecorder();

        _source.RaiseKey(65, 30, false, false);
        _source.RaiseKey(65, 30, false, false);
        _source.RaiseKey(65, 30, false, true);
        _source.RaiseKey(65, 30, false, true);

        var timeline = recorder.Stop(false);
        Assert.Equal(2, timeline.Events.Count);
        Assert.Equal(1, recorder.KeyCount);
    }

    [Fact]
    public void KeyCapture_ExcludesToolHotkeys()
    {
        var recorder = NewRecorder();

        _source.RaiseKey(0x7B, 0x44, false, false);
        _source.RaiseKey(0x7B, 0x44, false, true);
        _source.RaiseKey(0x7C, 0x45, false, false);
        _source.RaiseKey(0x7C, 0x45, false, true);
        _source.RaiseKey(0x7D, 0x46, false, false);
        _source.RaiseKey(0x7D, 0x46, false, true);

        var timeline = recorder.Stop(false);
        Assert.Empty(timeline.Events);
        Assert.Equal(0, recorder.KeyCount);
    }

    [Fact]
    public void MouseCapture_CapturesClickPairWithCoordinates()
    {
        var recorder = NewRecorder();

        _source.RaiseMouse(isDown: true, 100, 200);
        _source.RaiseMouse(isDown: true, 100, 200);
        _source.RaiseMouse(isDown: false, 100, 200);
        _source.RaiseMouse(isDown: false, 100, 200);

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

        _source.RaiseKey(65, 30, false, false);
        _source.RaiseMouse(isDown: true, 5, 5);
        _source.RaiseMouse(isDown: false, 5, 5);

        var timeline = recorder.Stop(dropTrailingMouse: true);

        Assert.Single(timeline.Events);
        Assert.True(timeline.Events[0].IsKey);
    }
}
