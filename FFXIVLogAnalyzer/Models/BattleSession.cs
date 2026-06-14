namespace FFXIVLogAnalyzer.Models;

public class BattleSession
{
    public int Index { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public int BossSkillCount { get; set; }
    public int PlayerSkillCount { get; set; }

    public TimeSpan Duration => EndTime > StartTime ? EndTime - StartTime : TimeSpan.Zero;

    public string DurationDisplay
    {
        get
        {
            var d = Duration;
            return d.TotalHours >= 1
                ? $"{(int)d.TotalHours}:{d.Minutes:D2}:{d.Seconds:D2}"
                : $"{d.Minutes}:{d.Seconds:D2}";
        }
    }

    public string DisplayName => $"#{Index + 1}  {StartTime:HH:mm:ss}  持续{DurationDisplay}  (Boss:{BossSkillCount} 玩家:{PlayerSkillCount})";
}
