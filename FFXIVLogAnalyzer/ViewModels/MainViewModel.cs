using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFXIVLogAnalyzer.Models;
using FFXIVLogAnalyzer.Services;
using Microsoft.Win32;

namespace FFXIVLogAnalyzer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly LogParser _parser = new();
    private readonly SkillMappingService _mappingService = new();

    private string _currentLogPath = string.Empty;
    private List<BossSkillEntry> _allBossSkills = new();
    private List<PlayerSkillEntry> _allPlayerSkills = new();
    private List<BossEffectEntry> _allBossEffects = new();

    [ObservableProperty] private string _windowTitle = "FFXIV 战斗日志分析器";
    [ObservableProperty] private string _statusText = "请打开日志文件";
    [ObservableProperty] private bool _hideIgnored;
    [ObservableProperty] private bool _requestOnly = true;
    [ObservableProperty] private string _selectedBossFilter = "全部";
    [ObservableProperty] private string _bossSearchText = string.Empty;
    [ObservableProperty] private string _effectSearchText = string.Empty;
    [ObservableProperty] private BattleSession? _selectedSession;
    [ObservableProperty] private string _selectedPlayerFilter = "全部";

    public ObservableCollection<BattleSession> Sessions { get; } = new();
    public ObservableCollection<string> PlayerNameFilters { get; } = new();
    public ObservableCollection<BossSkillEntry> BossSkills { get; } = new();
    public ObservableCollection<BossSkillEntry> FilteredBossSkills { get; } = new();
    public ObservableCollection<PlayerSkillEntry> NearbyPlayerSkills { get; } = new();
    public ObservableCollection<BossEffectEntry> NearbyBossEffects { get; } = new();
    public ObservableCollection<SkillMappingItem> SkillMappings { get; } = new();
    public ObservableCollection<string> BossNameFilters { get; } = new();
    public ObservableCollection<RawLogLine> RawLogLines { get; } = new();

    [ObservableProperty] private BossSkillEntry? _selectedBossSkill;
    [ObservableProperty] private BossEffectEntry? _selectedBossEffect;

    partial void OnSelectedBossSkillChanged(BossSkillEntry? value)
    {
        SelectedBossEffect = null;
        UpdateNearbyAll();
    }
    partial void OnHideIgnoredChanged(bool value) => UpdateNearbyAll();
    partial void OnRequestOnlyChanged(bool value) => UpdateNearbyAll();
    partial void OnSelectedBossFilterChanged(string value) => ApplyBossFilter();
    partial void OnBossSearchTextChanged(string value) => ApplyBossFilter();
    partial void OnEffectSearchTextChanged(string value) => UpdateNearbyEffects();
    partial void OnSelectedBossEffectChanged(BossEffectEntry? value) => UpdateNearbySkills();
    partial void OnSelectedSessionChanged(BattleSession? value) => OnSessionChanged();
    partial void OnSelectedPlayerFilterChanged(string value) => UpdateNearbySkills();

    /// <summary>The anchor time used for player skill Delta calculation.</summary>
    private DateTime GetPlayerAnchorTime()
    {
        if (SelectedBossEffect != null) return SelectedBossEffect.Timestamp;
        if (SelectedBossSkill != null) return SelectedBossSkill.Timestamp;
        return DateTime.MinValue;
    }

    private void UpdateNearbyAll() { UpdateNearbySkills(); UpdateNearbyEffects(); }
// PLACEHOLDER_CONTINUE

    [RelayCommand]
    private void OpenLog()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "日志文件 (*.log)|*.log|所有文件 (*.*)|*.*",
            Title = "选择战斗日志文件"
        };
        if (dlg.ShowDialog() != true) return;
        LoadLog(dlg.FileName);
    }

    public void LoadLog(string path)
    {
        _currentLogPath = path;
        var result = _parser.Parse(path);
        _allBossSkills = result.BossSkills;
        _allPlayerSkills = result.PlayerSkills;
        _allBossEffects = result.BossEffects;

        BossSkills.Clear(); FilteredBossSkills.Clear();
        NearbyPlayerSkills.Clear(); NearbyBossEffects.Clear();
        RawLogLines.Clear(); BossNameFilters.Clear(); SkillMappings.Clear();
        Sessions.Clear();

        // Sessions
        foreach (var s in result.Sessions) Sessions.Add(s);

        for (int i = 0; i < result.RawLines.Length; i++)
            RawLogLines.Add(new RawLogLine { LineNumber = i + 1, Text = result.RawLines[i] });

        // Global skill mappings: manual mappings override Action.csv names.
        var actionCsvMappings = _mappingService.LoadActionCsvMappings();
        var existingMappings = _mappingService.LoadMappings();
        var uniqueIds = result.PlayerSkills.Select(p => p.SkillId).Distinct().OrderBy(id => id);
        foreach (var id in uniqueIds)
        {
            var item = new SkillMappingItem { SkillId = id };
            if (existingMappings.TryGetValue(id, out var name))
                item.CustomName = name;
            else if (actionCsvMappings.TryGetValue(id, out var actionName))
                item.CustomName = actionName;
            SkillMappings.Add(item);
        }
        ApplyMappingsToSkills();

        WindowTitle = $"FFXIV 战斗日志分析器 - {Path.GetFileName(path)}";
        StatusText = $"已加载: {result.Sessions.Count} 场战斗, Boss技能 {result.BossSkills.Count}, 玩家技能 {result.PlayerSkills.Count}, Boss效果 {result.BossEffects.Count}";

        // Auto-select last session (most recent battle)
        if (Sessions.Count > 0)
            SelectedSession = Sessions.Last();
    }

    private void OnSessionChanged()
    {
        BossSkills.Clear();
        FilteredBossSkills.Clear();
        NearbyPlayerSkills.Clear();
        NearbyBossEffects.Clear();
        BossNameFilters.Clear();
        PlayerNameFilters.Clear();
        SelectedBossSkill = null;

        var sessionIdx = SelectedSession?.Index ?? -1;
        var sessionBoss = _allBossSkills.Where(b => b.SessionIndex == sessionIdx).ToList();
        foreach (var bs in sessionBoss) BossSkills.Add(bs);

        var bossNames = sessionBoss.Select(b => b.BossName).Distinct().ToList();
        BossNameFilters.Add("全部");
        foreach (var name in bossNames) BossNameFilters.Add(name);
        SelectedBossFilter = "全部";

        // Player name filters for this session
        var playerNames = _allPlayerSkills
            .Where(p => p.SessionIndex == sessionIdx)
            .Select(p => p.PlayerName)
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct().OrderBy(n => n).ToList();
        PlayerNameFilters.Add("全部");
        foreach (var name in playerNames) PlayerNameFilters.Add(name);
        SelectedPlayerFilter = "全部";

        ApplyBossFilter();
        UpdateNearbyAll();
    }

    private void ApplyBossFilter()
    {
        FilteredBossSkills.Clear();
        var source = BossSkills.AsEnumerable();
        if (SelectedBossFilter != "全部")
            source = source.Where(b => b.BossName == SelectedBossFilter);
        if (!string.IsNullOrWhiteSpace(BossSearchText))
        {
            var kw = BossSearchText.Trim();
            source = source.Where(b => b.SkillName.Contains(kw, StringComparison.OrdinalIgnoreCase)
                                     || b.SkillId.ToString().Contains(kw));
        }

        var deduped = source
            .GroupBy(b => new
            {
                b.EventKind,
                b.LineNumber,
                b.SessionIndex,
                b.Timestamp,
                b.SkillId,
                b.SkillName,
                b.BossName,
                b.EntityId,
                b.CastTime,
                b.IsTargetable
            })
            .Select(g => g.First())
            .OrderBy(b => b.Timestamp)
            .ThenBy(b => b.LineNumber);

        foreach (var bs in deduped) FilteredBossSkills.Add(bs);
    }

    private void UpdateNearbySkills()
    {
        NearbyPlayerSkills.Clear();
        if (SelectedBossSkill == null) return;
        var anchorTime = GetPlayerAnchorTime();
        var bossTime = SelectedBossSkill.Timestamp;
        var sessionIdx = SelectedSession?.Index ?? -1;
        var nearby = _allPlayerSkills
            .Where(p => p.SessionIndex == sessionIdx)
            .Where(p => Math.Abs((p.Timestamp - bossTime).TotalSeconds) <= 20)
            .Where(p => !RequestOnly || p.EventType == "Request")
            .Where(p => !HideIgnored || p.HighlightState != 2)
            .Where(p => SelectedPlayerFilter == "全部" || p.PlayerName == SelectedPlayerFilter)
            .OrderBy(p => p.Timestamp);
        foreach (var p in nearby)
        {
            p.DeltaSeconds = (p.Timestamp - anchorTime).TotalSeconds;
            NearbyPlayerSkills.Add(p);
        }
    }

    private void UpdateNearbyEffects()
    {
        NearbyBossEffects.Clear();
        if (SelectedBossSkill == null) return;
        var bossTime = SelectedBossSkill.Timestamp;
        var sessionIdx = SelectedSession?.Index ?? -1;
        var nearby = _allBossEffects
            .Where(e => e.SessionIndex == sessionIdx)
            .Where(e => Math.Abs((e.Timestamp - bossTime).TotalSeconds) <= 25);
        if (!string.IsNullOrWhiteSpace(EffectSearchText))
        {
            var kw = EffectSearchText.Trim();
            nearby = nearby.Where(e => e.ActionName.Contains(kw, StringComparison.OrdinalIgnoreCase)
                                     || e.ActionId.ToString().Contains(kw)
                                     || e.TargetName.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }
        // 同一时间同一ActionId的多次命中（AOE打多人）只显示一条，但收集所有目标名
        var deduped = nearby
            .GroupBy(e => e.EventKind == "Dialogue"
                ? new { e.EventKind, e.ActionId, e.Timestamp, e.LineNumber }
                : new { e.EventKind, e.ActionId, e.Timestamp, LineNumber = 0 })
            .Select(g =>
            {
                var first = g.First();
                first.AllTargetNames = g.Select(e => e.TargetName).Distinct().ToList();
                return first;
            })
            .OrderBy(e => e.Timestamp);
        foreach (var e in deduped)
        {
            e.DeltaSeconds = (e.Timestamp - bossTime).TotalSeconds;
            NearbyBossEffects.Add(e);
        }
    }
// PLACEHOLDER_COMMANDS

    /// <summary>
    /// Find the nearest boss skill or effect with the given ID, relative to a player skill timestamp.
    /// Returns a description string for display.
    /// </summary>
    public string QueryDistanceToBossEvent(PlayerSkillEntry playerSkill, string idText)
    {
        if (!int.TryParse(idText.Trim(), out var targetId))
            return "无效的ID";

        // Search both boss skills and boss effects
        DateTime? nearest = null;
        string? nearestName = null;
        string? nearestType = null;
        double minDist = double.MaxValue;

        foreach (var bs in BossSkills)
        {
            if (bs.SkillId == targetId)
            {
                var dist = Math.Abs((playerSkill.Timestamp - bs.Timestamp).TotalSeconds);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = bs.Timestamp;
                    nearestName = bs.SkillName;
                    nearestType = "Boss读条";
                }
            }
        }
        foreach (var ef in _allBossEffects)
        {
            if (ef.ActionId == targetId)
            {
                var dist = Math.Abs((playerSkill.Timestamp - ef.Timestamp).TotalSeconds);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = ef.Timestamp;
                    nearestName = ef.ActionName;
                    nearestType = "Boss效果";
                }
            }
        }

        if (nearest == null)
            return $"未找到ID为 {targetId} 的Boss技能或效果";

        var delta = (playerSkill.Timestamp - nearest.Value).TotalSeconds;
        var sign = delta >= 0 ? "+" : "";
        return $"距离最近的 [{nearestType}] {nearestName}({targetId}) @ {nearest.Value:HH:mm:ss.fff}: {sign}{delta:F1}s";
    }

    [RelayCommand]
    private void ToggleHighlight(PlayerSkillEntry? entry)
    {
        if (entry == null) return;
        entry.HighlightState = (entry.HighlightState + 1) % 3;
        if (HideIgnored && entry.HighlightState == 2)
            NearbyPlayerSkills.Remove(entry);
    }

    [RelayCommand]
    private void ApplyMappings()
    {
        var dict = SkillMappings
            .Where(m => !string.IsNullOrWhiteSpace(m.CustomName))
            .ToDictionary(m => m.SkillId, m => m.CustomName!);
        _mappingService.SaveMappings(dict);
        ApplyMappingsToSkills();
        UpdateNearbySkills();
        StatusText = $"已保存 {dict.Count} 个技能名称映射 (全局)";
    }

    private void ApplyMappingsToSkills()
    {
        var dict = SkillMappings
            .Where(m => !string.IsNullOrWhiteSpace(m.CustomName))
            .ToDictionary(m => m.SkillId, m => m.CustomName!);
        foreach (var p in _allPlayerSkills)
            p.DisplayName = dict.TryGetValue(p.SkillId, out var name) ? name : p.SkillId.ToString();
    }

    [RelayCommand]
    private void ExportTimeline()
    {
        if (BossSkills.Count == 0) return;
        var dlg = new SaveFileDialog
        {
            Filter = "文本文件 (*.txt)|*.txt",
            FileName = "timeline_export.txt",
            Title = "导出时间轴"
        };
        if (dlg.ShowDialog() != true) return;

        var sb = new StringBuilder();
        sb.AppendLine("# FFXIV 战斗时间轴导出");
        sb.AppendLine($"# 日志文件: {Path.GetFileName(_currentLogPath)}");
        sb.AppendLine();
        foreach (var boss in FilteredBossSkills)
        {
            sb.AppendLine($"# Boss: {boss.SkillName} ({boss.SkillId}) @ {boss.DisplayTime} 读条:{boss.CastTimeDisplay} [{boss.TargetableDisplay}]");
            var nearby = _allPlayerSkills
                .Where(p => Math.Abs((p.Timestamp - boss.Timestamp).TotalSeconds) <= 20)
                .Where(p => p.EventType == "Request" && p.HighlightState != 2)
                .OrderBy(p => p.Timestamp);
            foreach (var p in nearby)
            {
                var delta = (p.Timestamp - boss.Timestamp).TotalSeconds;
                var sign = delta >= 0 ? "+" : "";
                sb.AppendLine($"  {sign}{delta:F1}s  {p.SkillId}  {p.DisplayName}  {p.EventType}");
            }
            sb.AppendLine();
        }
        File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
        StatusText = $"时间轴已导出到: {dlg.FileName}";
    }
}

public class SkillMappingItem : ObservableObject
{
    public int SkillId { get; set; }
    private string? _customName;
    public string? CustomName
    {
        get => _customName;
        set => SetProperty(ref _customName, value);
    }
}

public class RawLogLine
{
    public int LineNumber { get; set; }
    public string Text { get; set; } = string.Empty;
}
