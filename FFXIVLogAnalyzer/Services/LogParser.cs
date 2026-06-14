using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using FFXIVLogAnalyzer.Models;

namespace FFXIVLogAnalyzer.Services;

public class LogParser
{
    private static readonly Regex BossSkillRegex = new(
        @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}) .+\[BattleLog\].+技能使用 (\d+) (.+?) 读条时间:([\d.]+) 来源于: Name: (.+?) DataId: (\d+) EntityId: (\d+) 可选中: (True|False)(?: 位置:([-\d.Ef]+),([-\d.Ef]+),([-\d.Ef]+))?",
        RegexOptions.Compiled);

    private static readonly Regex PlayerSkillRegex = new(
        @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}) .+技能触发Event (\d+) (Request|Effect)",
        RegexOptions.Compiled);

    private static readonly Regex BossEffectRegex = new(
        @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}) .+\[BattleLog\].+AbilityEffect ActionId: (\d+) Name: (.+?) Source: Name: (.+?) DataId: (\d+) .+Target: Name: (.+?) DataId: (\d+) .+可选中: (True|False)",
        RegexOptions.Compiled);

    private static readonly Regex BattleStartRegex = new(
        @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}) .+LogType: 57 Log: 战斗开始",
        RegexOptions.Compiled);

    private static readonly Regex WeatherRegex = new(
        @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}) .+\[BattleLog\].+天气Id变化 (\d+)",
        RegexOptions.Compiled);

    private static readonly Regex DialogueRegex = new(
        @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}) .+\[BattleLog\].+消息Log LogType: 68 Log: (.+)$",
        RegexOptions.Compiled);

    private static readonly Regex MapIdRegex = new(
        @"(?:TerritoryTypeId|MapId|地图ID|地图Id|地图id)\s*(?:is|:|：)?\s*(\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex TimestampRegex = new(
        @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3})",
        RegexOptions.Compiled);

    public record ParseResult(
        List<BossSkillEntry> BossSkills,
        List<PlayerSkillEntry> PlayerSkills,
        List<BossEffectEntry> BossEffects,
        List<BattleSession> Sessions,
        string[] RawLines);

    public ParseResult Parse(string filePath)
    {
        var lines = File.ReadAllLines(filePath, Encoding.UTF8);

        var sessionStarts = new List<(int lineIndex, DateTime time)>();
        for (var i = 0; i < lines.Length; i++)
        {
            var m = BattleStartRegex.Match(lines[i]);
            if (m.Success)
                sessionStarts.Add((i, ParseTimestamp(m.Groups[1].Value)));
        }

        if (sessionStarts.Count == 0)
            sessionStarts.Add((0, lines.Length > 0 ? TryParseLineTimestamp(lines[0]) : DateTime.MinValue));

        var sessions = BuildSessions(lines, sessionStarts);
        var mapIdsBySession = FindMapIdsBySession(lines, sessions);

        var bossSkills = new List<BossSkillEntry>();
        var playerSkills = new List<PlayerSkillEntry>();
        var bossEffects = new List<BossEffectEntry>();

        var currentSession = -1;
        for (var i = 0; i < lines.Length; i++)
        {
            while (currentSession + 1 < sessions.Count && i >= sessions[currentSession + 1].StartLine - 1)
                currentSession++;

            if (currentSession < 0)
                continue;

            var line = lines[i];
            var lineNumber = i + 1;

            var bossMatch = BossSkillRegex.Match(line);
            if (bossMatch.Success)
            {
                AddSkillEntry(bossMatch, line, lineNumber, currentSession, bossSkills, playerSkills);
                continue;
            }

            var playerMatch = PlayerSkillRegex.Match(line);
            if (playerMatch.Success)
            {
                playerSkills.Add(new PlayerSkillEntry
                {
                    LineNumber = lineNumber,
                    SessionIndex = currentSession,
                    Timestamp = ParseTimestamp(playerMatch.Groups[1].Value),
                    RawText = line,
                    SkillId = int.Parse(playerMatch.Groups[2].Value),
                    PlayerName = "自己",
                    EventType = playerMatch.Groups[3].Value,
                    DisplayName = playerMatch.Groups[2].Value
                });
                continue;
            }

            var effectMatch = BossEffectRegex.Match(line);
            if (effectMatch.Success)
            {
                AddBossEffect(effectMatch, line, lineNumber, currentSession, bossEffects);
                continue;
            }

            var weatherMatch = WeatherRegex.Match(line);
            if (weatherMatch.Success)
            {
                var weatherId = int.Parse(weatherMatch.Groups[2].Value);
                bossSkills.Add(new BossSkillEntry
                {
                    EventKind = "Weather",
                    LineNumber = lineNumber,
                    SessionIndex = currentSession,
                    Timestamp = ParseTimestamp(weatherMatch.Groups[1].Value),
                    RawText = line,
                    SkillId = weatherId,
                    SkillName = $"天气变化: {weatherId}",
                    BossName = "天气",
                    IsTargetable = false
                });
                continue;
            }

            var dialogueMatch = DialogueRegex.Match(line);
            if (dialogueMatch.Success)
            {
                var text = dialogueMatch.Groups[2].Value.Trim();
                bossEffects.Add(new BossEffectEntry
                {
                    EventKind = "Dialogue",
                    LineNumber = lineNumber,
                    SessionIndex = currentSession,
                    Timestamp = ParseTimestamp(dialogueMatch.Groups[1].Value),
                    RawText = line,
                    ActionId = 68,
                    ActionName = text,
                    SourceName = "台词",
                    TargetName = "台词"
                });
            }
        }

        AddMapEntries(bossSkills, sessions, mapIdsBySession);
        AddDirectionalSuffix(bossSkills);

        foreach (var s in sessions)
        {
            s.BossSkillCount = bossSkills.Count(b => b.SessionIndex == s.Index);
            s.PlayerSkillCount = playerSkills.Count(p => p.SessionIndex == s.Index);
        }

        return new ParseResult(bossSkills, playerSkills, bossEffects, sessions, lines);
    }

    private static List<BattleSession> BuildSessions(string[] lines, List<(int lineIndex, DateTime time)> sessionStarts)
    {
        var sessions = new List<BattleSession>();
        for (var s = 0; s < sessionStarts.Count; s++)
        {
            var startIdx = sessionStarts[s].lineIndex;
            var endIdx = s + 1 < sessionStarts.Count ? sessionStarts[s + 1].lineIndex : lines.Length;
            var endTime = sessionStarts[s].time;

            for (var j = endIdx - 1; j >= startIdx; j--)
            {
                var tm = TimestampRegex.Match(lines[j]);
                if (!tm.Success) continue;
                endTime = ParseTimestamp(tm.Groups[1].Value);
                break;
            }

            sessions.Add(new BattleSession
            {
                Index = s,
                StartTime = sessionStarts[s].time,
                EndTime = endTime,
                StartLine = startIdx + 1,
                EndLine = endIdx
            });
        }

        return sessions;
    }

    private static Dictionary<int, int?> FindMapIdsBySession(string[] lines, List<BattleSession> sessions)
    {
        var result = sessions.ToDictionary(s => s.Index, _ => (int?)null);
        var mapEvents = new List<(int lineIndex, int id)>();

        for (var i = 0; i < lines.Length; i++)
        {
            var m = MapIdRegex.Match(lines[i]);
            if (m.Success && int.TryParse(m.Groups[1].Value, out var id) && id > 0)
                mapEvents.Add((i, id));
        }

        foreach (var session in sessions)
        {
            var startIdx = session.StartLine - 1;
            var endIdx = session.EndLine;
            var map = mapEvents
                .Where(e => e.lineIndex < endIdx)
                .Where(e => e.lineIndex <= startIdx || e.lineIndex >= startIdx)
                .LastOrDefault();
            if (map.id > 0)
                result[session.Index] = map.id;
        }

        return result;
    }

    private static void AddSkillEntry(
        Match bossMatch,
        string line,
        int lineNumber,
        int currentSession,
        List<BossSkillEntry> bossSkills,
        List<PlayerSkillEntry> playerSkills)
    {
        var dataId = int.Parse(bossMatch.Groups[6].Value);
        if (dataId != 0)
        {
            var entityId = long.Parse(bossMatch.Groups[7].Value);
            float posX = 0, posZ = 0;
            if (bossMatch.Groups[9].Success)
            {
                posX = ParseFloat(bossMatch.Groups[9].Value);
                posZ = ParseFloat(bossMatch.Groups[11].Value);
            }

            bossSkills.Add(new BossSkillEntry
            {
                LineNumber = lineNumber,
                SessionIndex = currentSession,
                Timestamp = ParseTimestamp(bossMatch.Groups[1].Value),
                RawText = line,
                SkillId = int.Parse(bossMatch.Groups[2].Value),
                SkillName = bossMatch.Groups[3].Value.Trim(),
                CastTime = double.Parse(bossMatch.Groups[4].Value, CultureInfo.InvariantCulture),
                BossName = bossMatch.Groups[5].Value.Trim(),
                DataId = dataId,
                EntityId = entityId,
                IsTargetable = bossMatch.Groups[8].Value == "True",
                PosX = posX,
                PosZ = posZ
            });
        }
        else
        {
            playerSkills.Add(new PlayerSkillEntry
            {
                LineNumber = lineNumber,
                SessionIndex = currentSession,
                Timestamp = ParseTimestamp(bossMatch.Groups[1].Value),
                RawText = line,
                SkillId = int.Parse(bossMatch.Groups[2].Value),
                SkillName = bossMatch.Groups[3].Value.Trim(),
                PlayerName = bossMatch.Groups[5].Value.Trim(),
                EventType = "Cast",
                DisplayName = bossMatch.Groups[3].Value.Trim()
            });
        }
    }

    private static void AddBossEffect(
        Match effectMatch,
        string line,
        int lineNumber,
        int currentSession,
        List<BossEffectEntry> bossEffects)
    {
        var srcDataId = int.Parse(effectMatch.Groups[5].Value);
        if (srcDataId == 0)
            return;

        bossEffects.Add(new BossEffectEntry
        {
            LineNumber = lineNumber,
            SessionIndex = currentSession,
            Timestamp = ParseTimestamp(effectMatch.Groups[1].Value),
            RawText = line,
            ActionId = int.Parse(effectMatch.Groups[2].Value),
            ActionName = effectMatch.Groups[3].Value.Trim(),
            SourceName = effectMatch.Groups[4].Value.Trim(),
            SourceDataId = srcDataId,
            TargetName = effectMatch.Groups[6].Value.Trim(),
            TargetDataId = int.Parse(effectMatch.Groups[7].Value),
            TargetIsTargetable = effectMatch.Groups[8].Value == "True"
        });
    }

    private static void AddMapEntries(
        List<BossSkillEntry> bossSkills,
        List<BattleSession> sessions,
        Dictionary<int, int?> mapIdsBySession)
    {
        foreach (var session in sessions)
        {
            var first = bossSkills
                .Where(b => b.SessionIndex == session.Index)
                .OrderBy(b => b.Timestamp)
                .ThenBy(b => b.LineNumber)
                .FirstOrDefault();

            if (first == null)
                continue;

            var mapId = mapIdsBySession.TryGetValue(session.Index, out var id) ? id : null;
            var text = mapId.HasValue ? $"地图ID: {mapId.Value}" : "地图ID: 未知";
            bossSkills.Add(new BossSkillEntry
            {
                EventKind = "Map",
                LineNumber = session.StartLine,
                SessionIndex = session.Index,
                Timestamp = first.Timestamp.AddMilliseconds(-1),
                RawText = text,
                SkillName = text,
                BossName = "地图"
            });
        }
    }

    private static DateTime ParseTimestamp(string ts)
    {
        return DateTime.ParseExact(ts, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
    }

    private static DateTime TryParseLineTimestamp(string line)
    {
        if (line.Length >= 23 && DateTime.TryParseExact(line[..23], "yyyy-MM-dd HH:mm:ss.fff",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;
        return DateTime.MinValue;
    }

    private static float ParseFloat(string s)
    {
        var clean = s.TrimEnd('f', 'F');
        return float.Parse(clean, CultureInfo.InvariantCulture);
    }

    private static void AddDirectionalSuffix(List<BossSkillEntry> bossSkills)
    {
        var untargetable = bossSkills.Where(b => b.EventKind == "BossCast" && !b.IsTargetable).ToList();
        var sessionGroups = untargetable.GroupBy(b => b.SessionIndex);

        foreach (var session in sessionGroups)
        {
            var nameGroups = session.GroupBy(b => b.BossName);
            foreach (var nameGroup in nameGroups)
            {
                var distinctEntities = nameGroup.Select(b => b.EntityId).Distinct().ToList();
                if (distinctEntities.Count <= 1) continue;

                var entityPositions = new Dictionary<long, (float x, float z)>();
                foreach (var entityId in distinctEntities)
                {
                    var first = nameGroup.First(b => b.EntityId == entityId);
                    entityPositions[entityId] = (first.PosX, first.PosZ);
                }

                var positions = entityPositions.Values.ToList();
                if (positions.All(p => MathF.Abs(p.x - positions[0].x) < 1f && MathF.Abs(p.z - positions[0].z) < 1f))
                    continue;

                const float centerX = 100f, centerZ = 100f;
                var entityDirs = new Dictionary<long, (string dir, int seq)>();
                var directionCounts = new Dictionary<string, int>();

                foreach (var (entityId, (x, z)) in entityPositions)
                {
                    var dir = GetDirection(x, z, centerX, centerZ);
                    directionCounts.TryGetValue(dir, out var count);
                    directionCounts[dir] = count + 1;
                    entityDirs[entityId] = (dir, count + 1);
                }

                var finalLabels = new Dictionary<long, string>();
                foreach (var (entityId, (dir, seq)) in entityDirs)
                {
                    finalLabels[entityId] = directionCounts[dir] == 1 ? dir : dir + seq;
                }

                foreach (var entry in nameGroup)
                {
                    if (finalLabels.TryGetValue(entry.EntityId, out var suffix))
                        entry.BossName = $"{entry.BossName}({suffix})";
                }
            }
        }
    }

    private static string GetDirection(float x, float z, float cx, float cz)
    {
        var dx = x - cx;
        var dz = z - cz;

        if (MathF.Abs(dx) < 3f && MathF.Abs(dz) < 3f)
            return "中";

        var angle = MathF.Atan2(dz, dx);
        var deg = angle * 180f / MathF.PI;

        return deg switch
        {
            >= -22.5f and < 22.5f => "东",
            >= 22.5f and < 67.5f => "东南",
            >= 67.5f and < 112.5f => "南",
            >= 112.5f and < 157.5f => "西南",
            >= 157.5f or < -157.5f => "西",
            >= -157.5f and < -112.5f => "西北",
            >= -112.5f and < -67.5f => "北",
            >= -67.5f and < -22.5f => "东北",
            _ => "中"
        };
    }
}
