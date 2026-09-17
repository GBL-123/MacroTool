namespace MacroTool.Infrastructure.Interop;

public static class WindowsEnvironment
{
    public static bool IsElevated => OperatingSystem.IsWindows() && Native.IsUserAnAdmin();

    public static void EnableDpiAwareness() => Native.SetProcessDPIAware();
}
