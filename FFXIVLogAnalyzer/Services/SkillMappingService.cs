using System.IO;
using System.Reflection;
using System.Text.Json;

namespace FFXIVLogAnalyzer.Services;

public class SkillMappingService
{
    private static readonly string GlobalMappingPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FFXIVLogAnalyzer", "skill_mappings.json");

    public Dictionary<int, string> LoadMappings()
    {
        if (!File.Exists(GlobalMappingPath))
            return new Dictionary<int, string>();

        var json = File.ReadAllText(GlobalMappingPath);
        return JsonSerializer.Deserialize<Dictionary<int, string>>(json) ?? new Dictionary<int, string>();
    }

    public Dictionary<int, string> LoadActionCsvMappings()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(".Action.csv", StringComparison.OrdinalIgnoreCase));
        if (resourceName == null)
            return new Dictionary<int, string>();

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            return new Dictionary<int, string>();

        using var reader = new StreamReader(stream);
        var mappings = new Dictionary<int, string>();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var fields = ReadFirstCsvFields(line, 2);
            if (fields.Count < 2 || !int.TryParse(fields[0], out var id))
                continue;

            var name = fields[1].Trim();
            if (!string.IsNullOrWhiteSpace(name))
                mappings[id] = name;
        }

        return mappings;
    }

    public void SaveMappings(Dictionary<int, string> mappings)
    {
        var dir = Path.GetDirectoryName(GlobalMappingPath)!;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(mappings, options);
        File.WriteAllText(GlobalMappingPath, json);
    }

    private static List<string> ReadFirstCsvFields(string line, int maxFields)
    {
        var fields = new List<string>(maxFields);
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                if (fields.Count == maxFields)
                    return fields;
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }
}
