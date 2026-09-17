namespace MacroTool.Web.Hosting;

public sealed class MacroOptions
{
    public const string SectionName = "Macro";

    public double Speed { get; set; } = 1.0;

    public int Jitter { get; set; } = 2;

    public int RepeatCount { get; set; } = 0;

    public string TimelinePath { get; set; } = "";

    public bool OpenBrowser { get; set; } = true;

    public NormalizedMacroOptions Normalize()
    {
        var warnings = new List<string>();

        double speed = Speed;
        if (double.IsNaN(speed) || double.IsInfinity(speed) || speed <= 0)
        {
            warnings.Add($"配置 Macro:Speed 无效({Speed})，回退为 1.0");
            speed = 1.0;
        }

        int jitter = Jitter;
        if (jitter < 0)
        {
            warnings.Add($"配置 Macro:Jitter 无效({Jitter})，回退为 2");
            jitter = 2;
        }

        int repeatCount = RepeatCount;
        if (repeatCount < 0)
        {
            warnings.Add($"配置 Macro:RepeatCount 无效({RepeatCount})，回退为 0（无限循环）");
            repeatCount = 0;
        }

        string? timelinePath = string.IsNullOrWhiteSpace(TimelinePath) ? null : TimelinePath.Trim();
        return new NormalizedMacroOptions(speed, jitter, repeatCount, timelinePath, OpenBrowser, warnings);
    }
}

public sealed record NormalizedMacroOptions(
    double Speed,
    int Jitter,
    int RepeatCount,
    string? TimelinePath,
    bool OpenBrowser,
    IReadOnlyList<string> Warnings);
