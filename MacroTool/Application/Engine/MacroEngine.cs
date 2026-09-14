using MacroTool.Application.Hotkeys;
using MacroTool.Application.Interop;
using MacroTool.Application.Storage;
using Microsoft.Extensions.Logging;

namespace MacroTool.Application.Engine;

public readonly record struct EngineSnapshot(
    ToolState State,
    int EventCount,
    double DurationMs,
    int ClickCount,
    int KeyCount,
    double RecordingElapsedMs,
    int PlaybackRound,
    int PlaybackRepeat,
    double PlaybackElapsedMs,
    bool HasTimeline);

public sealed class MacroEngine : IDisposable
{
    private readonly object _gate = new();
    private readonly EngineStateMachine _state = new();
    private readonly HotkeyHost _hotkeys;
    private readonly TimelineStore _store;
    private readonly EngineSettings _settings;
    private readonly LogBuffer _logBuffer;
    private readonly ILogger<MacroEngine> _logger;
    private readonly PlayerDelegates? _playerDelegates;
    private readonly bool _isElevated;

    private Recorder? _recorder;
    private Player? _player;
    private MacroTimeline? _timeline;
    private bool _started;
    private bool _shutdownRequested;
    private int _disposed;

    public MacroEngine(
        HotkeyHost hotkeys,
        TimelineStore store,
        EngineSettings settings,
        LogBuffer logBuffer,
        ILogger<MacroEngine> logger,
        PlayerDelegates? playerDelegates = null)
    {
        _hotkeys = hotkeys;
        _store = store;
        _settings = settings;
        _logBuffer = logBuffer;
        _logger = logger;
        _playerDelegates = playerDelegates;
        _isElevated = OperatingSystem.IsWindows() && Native.IsUserAnAdmin();
    }

    public event Action? StateChanged;

    public event Action? TimelineChanged;

    public event Action? ShutdownRequested;

    public ToolState State => _state.State;

    public HotkeyStatus HotkeyStatus => _hotkeys.Status;

    public bool IsElevated => _isElevated;

    public MacroTimeline? CurrentTimeline
    {
        get
        {
            lock (_gate)
                return _timeline;
        }
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_started)
                return;
            _started = true;

            _hotkeys.OnF10 = ToggleRecording;
            _hotkeys.OnF11 = TogglePlayback;
            _hotkeys.OnF12 = () => RequestShutdown(fromUi: false);
            _hotkeys.StatusChanged += OnHotkeyStatusChanged;

            var load = _store.Load();
            if (load.Error is not null)
                Log(LogLevel.Error, $"读取时间线失败: {load.Error}");

            _timeline = load.Timeline;
            if (_timeline is null)
                Log(LogLevel.Information, "尚无时间线，按 F10 或页面按钮开始录制");
            else
                Log(LogLevel.Information, $"已加载时间线: {_timeline.Events.Count} 事件 / {_timeline.DurationMs:F0}ms");

            _hotkeys.Start();
        }
    }

    public void ToggleRecording()
    {
        lock (_gate)
        {
            if (_state.State == ToolState.Recording)
                StopRecordingInternal(fromUi: false);
            else
                StartRecordingInternal();
        }
    }

    public void TogglePlayback()
    {
        lock (_gate)
        {
            if (_state.State == ToolState.Playing)
                StopPlaybackInternal();
            else
                StartPlaybackInternal();
        }
    }

    public void StartRecording()
    {
        lock (_gate)
            StartRecordingInternal();
    }

    public void StopRecording(bool fromUi)
    {
        lock (_gate)
            StopRecordingInternal(fromUi);
    }

    public void StartPlayback()
    {
        lock (_gate)
            StartPlaybackInternal();
    }

    public void StopPlayback()
    {
        lock (_gate)
            StopPlaybackInternal();
    }

    public void StopAll(bool fromUi)
    {
        lock (_gate)
        {
            if (_state.State == ToolState.Recording)
                StopRecordingInternal(fromUi);
            if (_state.State == ToolState.Playing)
                StopPlaybackInternal();
        }
    }

    public void RequestShutdown(bool fromUi)
    {
        lock (_gate)
        {
            if (_shutdownRequested)
                return;
            _shutdownRequested = true;

            if (_state.State == ToolState.Recording)
                StopRecordingInternal(fromUi);
            if (_state.State == ToolState.Playing)
                StopPlaybackInternal();
        }

        Log(LogLevel.Information, fromUi ? "收到关闭请求（控制面板）" : "收到关闭请求（F12）");
        ShutdownRequested?.Invoke();
    }

    public bool UpdateSettings(double speed, int jitter, int repeatCount, out string? error)
    {
        if (double.IsNaN(speed) || double.IsInfinity(speed) || speed <= 0)
        {
            error = "倍速必须大于 0";
            return false;
        }
        if (jitter < 0)
        {
            error = "抖动不能为负数";
            return false;
        }
        if (repeatCount < 0)
        {
            error = "回放次数不能为负数（0 表示无限循环）";
            return false;
        }

        _settings.Speed = speed;
        _settings.Jitter = jitter;
        _settings.RepeatCount = repeatCount;
        error = null;
        Log(LogLevel.Information, $"参数更新: speed={speed:0.##}，jitter={jitter}px，次数={(repeatCount > 0 ? repeatCount.ToString() : "无限")}（回放中从下一轮生效）");
        return true;
    }

    public void ReplaceTimeline(MacroTimeline timeline)
    {
        lock (_gate)
        {
            if (_state.State != ToolState.Idle)
            {
                Log(LogLevel.Warning, "引擎忙，已忽略时间线替换");
                return;
            }
            _timeline = timeline;
        }

        Log(LogLevel.Information, $"时间线已更新: {timeline.Events.Count} 事件 / {timeline.DurationMs:F0}ms");
        TimelineChanged?.Invoke();
    }

    public EngineSnapshot GetSnapshot()
    {
        lock (_gate)
        {
            var timeline = _timeline;
            return new EngineSnapshot(
                _state.State,
                timeline?.Events.Count ?? 0,
                timeline?.DurationMs ?? 0,
                _recorder?.ClickCount ?? 0,
                _recorder?.KeyCount ?? 0,
                _recorder?.ElapsedMs ?? 0,
                _player?.Round ?? 0,
                _settings.RepeatCount,
                _player?.ElapsedMs ?? 0,
                timeline is { Events.Count: > 0 });
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        lock (_gate)
        {
            if (_state.State == ToolState.Recording)
                StopRecordingInternal(fromUi: false);
            if (_state.State == ToolState.Playing)
                StopPlaybackInternal();
            _hotkeys.StatusChanged -= OnHotkeyStatusChanged;
        }

        _hotkeys.Dispose();
    }

    private void StartRecordingInternal()
    {
        if (!_state.TryBeginRecording(out var reason))
        {
            Log(LogLevel.Warning, reason!);
            return;
        }

        _recorder = new Recorder();
        _recorder.Start();
        Log(LogLevel.Information, "录制开始（F10 或页面按钮停止，操作原样透传）");
        StateChanged?.Invoke();
    }

    private void StopRecordingInternal(bool fromUi)
    {
        if (!_state.TryEndRecording(out var reason))
        {
            Log(LogLevel.Warning, reason!);
            return;
        }

        var recorder = _recorder;
        _recorder = null;
        if (recorder is null)
        {
            StateChanged?.Invoke();
            return;
        }

        var timeline = recorder.Stop(fromUi);
        _timeline = timeline;

        int clicks = timeline.Events.Count(e => !e.IsKey && !e.Up);
        int keys = timeline.Events.Count(e => e.IsKey && !e.Up);
        var save = _store.Save(timeline, _settings.Speed);
        if (save.Success)
            Log(LogLevel.Information, $"录制停止: {clicks} 次点击 / {keys} 次按键 / {timeline.DurationMs:F0}ms，已保存 {Path.GetFileName(_store.FilePath)}");
        else
            Log(LogLevel.Error, $"录制已停止但保存失败: {save.Error}（时间线保留在内存中，可重试）");

        StateChanged?.Invoke();
        TimelineChanged?.Invoke();
    }

    private void StartPlaybackInternal()
    {
        var timeline = _timeline;
        if (!_state.TryBeginPlayback(timeline is { Events.Count: > 0 }, out var reason))
        {
            Log(LogLevel.Warning, reason!);
            return;
        }

        var player = new Player(timeline!, _settings, _playerDelegates);
        player.RoundCompleted += round => Log(LogLevel.Information, $"回放第 {round} 轮完成");
        player.InjectionFailed += () => Log(LogLevel.Warning, "输入注入失败（若游戏以管理员运行，本工具需同样提权）");
        player.Completed += OnPlaybackCompleted;
        _player = player;
        player.Start();
        int repeat = _settings.RepeatCount;
        Log(LogLevel.Information, $"回放开始（speed={_settings.Speed:0.##}，jitter={_settings.Jitter}px，次数={(repeat > 0 ? repeat.ToString() : "无限")}，F11 或页面停止）");
        StateChanged?.Invoke();
    }

    private void OnPlaybackCompleted(int rounds)
    {
        lock (_gate)
        {
            if (_state.State != ToolState.Playing)
                return;
            // 保留 _player 引用：完成后快照仍能上报最终轮数（UI 在回放态之外不展示该值）
            _state.TryEndPlayback(out _);
        }

        Log(LogLevel.Information, $"回放完成，共 {rounds} 轮");
        StateChanged?.Invoke();
    }

    private void StopPlaybackInternal()
    {
        if (!_state.TryEndPlayback(out var reason))
        {
            Log(LogLevel.Warning, reason!);
            return;
        }

        var player = _player;
        _player = null;
        player?.Stop();
        Log(LogLevel.Information, "回放已停止");
        StateChanged?.Invoke();
    }

    private void OnHotkeyStatusChanged()
    {
        var status = _hotkeys.Status;
        if (!status.F10Registered || !status.F11Registered)
        {
            var missing = status is { F10Registered: false, F11Registered: false }
                ? "F10/F11"
                : status.F10Registered ? "F11" : "F10";
            Log(LogLevel.Warning, $"{missing} 热键注册失败（可能被其他程序占用），仍可通过控制面板控制");
        }
        else
        {
            Log(LogLevel.Information, "F10/F11 热键已注册");
        }

        if (!status.ExitKeyAvailable)
            Log(LogLevel.Warning, "F12 被占用，退出请使用控制面板的关闭服务按钮");
        else
            Log(LogLevel.Information, $"退出热键: {status.ExitKeyLabel}");
    }

    private void Log(LogLevel level, string message)
    {
        _logger.Log(level, "{Message}", message);
        _logBuffer.Add(new EngineLogEntry(DateTimeOffset.Now, level, message));
    }
}
