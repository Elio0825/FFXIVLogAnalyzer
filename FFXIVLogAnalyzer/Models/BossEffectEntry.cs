namespace FFXIVLogAnalyzer.Models;

public class BossEffectEntry : LogEntry
{
    public string EventKind { get; set; } = "AbilityEffect";
    public int ActionId { get; set; }
    public string ActionName { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public int SourceDataId { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public int TargetDataId { get; set; }
    public bool TargetIsTargetable { get; set; }

    /// <summary>AOE命中多人时，存储所有被命中目标的名称列表</summary>
    public List<string> AllTargetNames { get; set; } = new();

    /// <summary>目标列显示：单目标直接显示名称，多目标显示"名称 (+N)"</summary>
    public string TargetDisplay => EventKind == "Dialogue"
        ? "台词"
        : AllTargetNames.Count <= 1
        ? TargetName
        : $"{TargetName} (+{AllTargetNames.Count - 1})";

    /// <summary>ToolTip显示完整目标列表</summary>
    public string TargetTooltip => EventKind == "Dialogue"
        ? RawText
        : AllTargetNames.Count <= 1
        ? TargetName
        : string.Join("\n", AllTargetNames);

    public string DisplayTime => Timestamp.ToString("HH:mm:ss.fff");
    public double DeltaSeconds { get; set; }
    public string DeltaDisplay => DeltaSeconds >= 0 ? $"+{DeltaSeconds:F1}s" : $"{DeltaSeconds:F1}s";
}
