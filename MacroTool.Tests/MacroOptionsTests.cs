using MacroTool.Application;

namespace MacroTool.Tests;

public sealed class MacroOptionsTests
{
    [Fact]
    public void Normalize_Defaults_AreValid()
    {
        var normalized = new MacroOptions().Normalize();

        Assert.Equal(1.0, normalized.Speed);
        Assert.Equal(2, normalized.Jitter);
        Assert.Null(normalized.TimelinePath);
        Assert.True(normalized.OpenBrowser);
        Assert.Empty(normalized.Warnings);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Normalize_InvalidSpeed_FallsBackWithWarning(double speed)
    {
        var normalized = new MacroOptions { Speed = speed }.Normalize();

        Assert.Equal(1.0, normalized.Speed);
        Assert.Single(normalized.Warnings);
    }

    [Fact]
    public void Normalize_NegativeJitter_FallsBackWithWarning()
    {
        var normalized = new MacroOptions { Jitter = -3 }.Normalize();

        Assert.Equal(2, normalized.Jitter);
        Assert.Single(normalized.Warnings);
    }

    [Fact]
    public void Normalize_NegativeRepeatCount_FallsBackWithWarning()
    {
        var normalized = new MacroOptions { RepeatCount = -2 }.Normalize();

        Assert.Equal(0, normalized.RepeatCount);
        Assert.Single(normalized.Warnings);
    }

    [Fact]
    public void Normalize_WhitespaceTimelinePath_BecomesNull()
    {
        var normalized = new MacroOptions { TimelinePath = "   " }.Normalize();

        Assert.Null(normalized.TimelinePath);
    }

    [Fact]
    public void Normalize_CustomValues_ArePreservedAndTrimmed()
    {
        var normalized = new MacroOptions
        {
            Speed = 2.5,
            Jitter = 0,
            TimelinePath = " D:\\macros\\timeline.json ",
            OpenBrowser = false
        }.Normalize();

        Assert.Equal(2.5, normalized.Speed);
        Assert.Equal(0, normalized.Jitter);
        Assert.Equal("D:\\macros\\timeline.json", normalized.TimelinePath);
        Assert.False(normalized.OpenBrowser);
        Assert.Empty(normalized.Warnings);
    }
}
