using MacroTool.Application.Engine;

namespace MacroTool.Tests;

public sealed class EngineSettingsTests
{
    [Fact]
    public void Values_RoundTrip()
    {
        var settings = new EngineSettings(1.0, 2);

        settings.Speed = 3.5;
        settings.Jitter = 7;

        Assert.Equal(3.5, settings.Speed);
        Assert.Equal(7, settings.Jitter);
    }

    [Fact]
    public void ConcurrentReadWrite_DoesNotThrow()
    {
        var settings = new EngineSettings(1.0, 0);
        var stop = new ManualResetEventSlim(false);

        var writer = new Thread(() =>
        {
            for (var i = 0; i < 20000; i++)
            {
                settings.Speed = 1.0 + (i % 10);
                settings.Jitter = i % 5;
            }
            stop.Set();
        });

        var reader = new Thread(() =>
        {
            while (!stop.IsSet)
            {
                var speed = settings.Speed;
                var jitter = settings.Jitter;
                Assert.True(speed > 0 || speed == 1.0 + 9);
                Assert.InRange(jitter, 0, 4);
            }
        });

        writer.Start();
        reader.Start();
        Assert.True(writer.Join(10000));
        Assert.True(reader.Join(10000));
    }
}
