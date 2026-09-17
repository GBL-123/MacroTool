using MacroTool.Infrastructure.Interop;

namespace MacroTool.Tests;

public sealed class LowLevelHookCaptureTests
{
    [Theory]
    [InlineData(0x0201, true, true)]
    [InlineData(0x0202, true, false)]
    [InlineData(0x0204, false, false)]
    [InlineData(0x0207, false, false)]
    public void TryTranslateLeftButton_OnlyAcceptsLeftButtonMessages(int msg, bool expected, bool expectedDown)
    {
        Assert.Equal(expected, LowLevelHookCapture.TryTranslateLeftButton(msg, out bool isDown));
        if (expected)
            Assert.Equal(expectedDown, isDown);
    }
}
