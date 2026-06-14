namespace FFXIVLogAnalyzer.Models;

public class BossSkillEntry : LogEntry
{
    public string EventKind { get; set; } = "BossCast";
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public double CastTime { get; set; }
    public string BossName { get; set; } = string.Empty;
    public int DataId { get; set; }
    public long EntityId { get; set; }
    public bool IsTargetable { get; set; }
    public float PosX { get; set; }
    public float PosZ { get; set; }

    public bool IsBossCast => EventKind == "BossCast";
    public string DisplayTime => Timestamp.ToString("HH:mm:ss.fff");
    public string CastTimeDisplay => IsBossCast ? $"{CastTime:F1}s" : string.Empty;
    public string TargetableDisplay => EventKind switch
    {
        "Weather" => "天气",
        "Map" => "地图",
        _ => IsTargetable ? "可选中" : "不可选中"
    };
}
