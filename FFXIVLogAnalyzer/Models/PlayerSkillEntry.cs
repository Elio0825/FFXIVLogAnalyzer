using CommunityToolkit.Mvvm.ComponentModel;

namespace FFXIVLogAnalyzer.Models;

public partial class PlayerSkillEntry : ObservableObject
{
    public int LineNumber { get; set; }
    public DateTime Timestamp { get; set; }
    public string RawText { get; set; } = string.Empty;
    public int SessionIndex { get; set; }
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty; // from BattleLog parsing
    public string PlayerName { get; set; } = string.Empty; // empty = self (技能触发Event)
    public string EventType { get; set; } = string.Empty; // Request, Effect, or Cast

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private double _deltaSeconds;

    [ObservableProperty]
    private int _highlightState; // 0=Normal, 1=Highlighted, 2=Ignored

    public string DisplayTime => Timestamp.ToString("HH:mm:ss.fff");
    public string DeltaDisplay => DeltaSeconds >= 0 ? $"+{DeltaSeconds:F1}s" : $"{DeltaSeconds:F1}s";
}
