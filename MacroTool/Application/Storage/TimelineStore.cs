using System.Text.Json;
using System.Text.Json.Serialization;
using MacroTool.Application.Engine;

namespace MacroTool.Application.Storage;

public sealed record TimelineLoadResult(MacroTimeline? Timeline, string? Error)
{
    public bool Success => Error is null;
}

public sealed record TimelineSaveResult(bool Success, string? Error);

public sealed class TimelineStore
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private readonly string _filePath;

    public TimelineStore(string filePath)
    {
        _filePath = filePath;
    }

    public string FilePath => _filePath;

    public string BackupPath => _filePath + ".bak";

    public bool Exists => File.Exists(_filePath);

    public static string DefaultPath => Path.Combine(AppContext.BaseDirectory, "timeline.json");

    public static string ResolvePath(string? configuredPath)
        => string.IsNullOrWhiteSpace(configuredPath)
            ? DefaultPath
            : Path.GetFullPath(configuredPath, AppContext.BaseDirectory);

    public TimelineLoadResult Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new TimelineLoadResult(null, null);

            var dto = JsonSerializer.Deserialize<TimelineFileDto>(File.ReadAllText(_filePath));
            if (dto is null)
                return new TimelineLoadResult(null, "时间线文件内容为空或不是有效 JSON");

            var timeline = new MacroTimeline { DurationMs = dto.DurationMs };
            foreach (var e in dto.Events)
            {
                timeline.Events.Add(new MacroEvent
                {
                    T = e.T,
                    IsKey = string.Equals(e.Type, "key", StringComparison.OrdinalIgnoreCase),
                    Vk = e.Vk,
                    Scan = e.Scan,
                    Ext = e.Ext,
                    Up = e.Up,
                    X = e.X,
                    Y = e.Y
                });
            }
            return new TimelineLoadResult(timeline, null);
        }
        catch (Exception ex)
        {
            return new TimelineLoadResult(null, ex.Message);
        }
    }

    public TimelineSaveResult Save(MacroTimeline timeline, double speedHint)
    {
        try
        {
            string? dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            if (File.Exists(_filePath))
                File.Copy(_filePath, BackupPath, overwrite: true);

            string json = JsonSerializer.Serialize(ToDto(timeline, speedHint), JsonOpts);
            string temp = _filePath + ".tmp";
            try
            {
                File.WriteAllText(temp, json);
                File.Move(temp, _filePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }

            return new TimelineSaveResult(true, null);
        }
        catch (Exception ex)
        {
            return new TimelineSaveResult(false, ex.Message);
        }
    }

    private static TimelineFileDto ToDto(MacroTimeline timeline, double speedHint) => new()
    {
        RecordedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        SpeedHint = speedHint,
        DurationMs = timeline.DurationMs,
        Events = timeline.Events.Select(e => new EventDto
        {
            T = Math.Round(e.T, 1),
            Type = e.IsKey ? "key" : "click",
            Vk = e.Vk,
            Scan = e.Scan,
            Ext = e.Ext,
            Up = e.Up,
            X = e.X,
            Y = e.Y
        }).ToList()
    };
}

public sealed class TimelineFileDto
{
    [JsonPropertyName("recordedAt")]
    public string RecordedAt { get; set; } = "";

    [JsonPropertyName("speedHint")]
    public double SpeedHint { get; set; } = 1.0;

    [JsonPropertyName("durationMs")]
    public double DurationMs { get; set; }

    [JsonPropertyName("events")]
    public List<EventDto> Events { get; set; } = [];
}

public sealed class EventDto
{
    [JsonPropertyName("t")]
    public double T { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("vk")]
    public int Vk { get; set; }

    [JsonPropertyName("scan")]
    public int Scan { get; set; }

    [JsonPropertyName("ext")]
    public bool Ext { get; set; }

    [JsonPropertyName("up")]
    public bool Up { get; set; }

    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }
}
