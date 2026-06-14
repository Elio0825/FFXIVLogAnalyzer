namespace FFXIVLogAnalyzer.Models;

public class LogEntry
{
    public int LineNumber { get; set; }
    public DateTime Timestamp { get; set; }
    public string RawText { get; set; } = string.Empty;
    public int SessionIndex { get; set; }
}
