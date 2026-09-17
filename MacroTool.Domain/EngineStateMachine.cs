namespace MacroTool.Domain;

public enum ToolState
{
    Idle,
    Recording,
    Playing
}

public sealed class EngineStateMachine
{
    public ToolState State { get; private set; } = ToolState.Idle;

    public bool TryBeginRecording(out string? rejection)
    {
        if (State == ToolState.Playing)
        {
            rejection = "回放中不能录制，请先停止回放";
            return false;
        }
        if (State == ToolState.Recording)
        {
            rejection = "已在录制中";
            return false;
        }
        rejection = null;
        State = ToolState.Recording;
        return true;
    }

    public bool TryBeginPlayback(bool hasPlayableTimeline, out string? rejection)
    {
        if (State == ToolState.Recording)
        {
            rejection = "录制中不能回放，请先停止录制";
            return false;
        }
        if (State == ToolState.Playing)
        {
            rejection = "已在回放中";
            return false;
        }
        if (!hasPlayableTimeline)
        {
            rejection = "没有可回放的时间线，请先录制";
            return false;
        }
        rejection = null;
        State = ToolState.Playing;
        return true;
    }

    public bool TryEndRecording(out string? rejection)
    {
        if (State != ToolState.Recording)
        {
            rejection = "当前不在录制中";
            return false;
        }
        rejection = null;
        State = ToolState.Idle;
        return true;
    }

    public bool TryEndPlayback(out string? rejection)
    {
        if (State != ToolState.Playing)
        {
            rejection = "当前不在回放中";
            return false;
        }
        rejection = null;
        State = ToolState.Idle;
        return true;
    }
}
