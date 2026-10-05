using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Hope.Launcher;

public sealed class ModDefinition
{
    public int FormatVersion { get; set; }
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
    public Dictionary<string, JsonElement> Settings { get; set; } = [];
    [JsonIgnore] public string Changes => string.Join(" • ", Settings.Select(p => p.Key + " = " + p.Value));
}
public sealed class ModChoice
{
    public required ModDefinition Definition { get; init; }
    public bool Enabled { get; set; }
    public string Name => Definition.Name;
    public string Description => Definition.Description;
    public string Credit => "By " + Definition.Author;
    public string Changes => Definition.Changes;
}
public sealed class ModState
{
    public string[] Enabled { get; set; } = [];
    public Dictionary<string, string?> Originals { get; set; } = [];
    public Dictionary<string, string> Applied { get; set; } = [];
}
public sealed class ModTransaction
{
    public string? BeforeSettings { get; set; }
    public string? BeforeState { get; set; }
    public string AfterSettingsHash { get; set; } = "";
    public string AfterState { get; set; } = "";
}
public sealed class ModManager(string bundleRoot, string careerRoot, Func<bool> gameRunning)
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    private string Library => Path.Combine(bundleRoot, "mods", "imported");
    private string Store => Path.Combine(careerRoot, "mods");
    private string Settings => Path.Combine(careerRoot, "settings.toml");
    private string StatePath => Path.Combine(Store, "state.json");
    private string Journal => Path.Combine(Store, "transaction.json");
    public string Warning { get; private set; } = "";
    public static IReadOnlyList<ModDefinition> BuiltIns { get; } = LoadBuiltIns();

    private static IReadOnlyList<ModDefinition> LoadBuiltIns()
    {
        var assembly = typeof(ModManager).Assembly;
        return assembly.GetManifestResourceNames().Where(n => n.EndsWith(".hope-mod.json", StringComparison.Ordinal))
            .Order().Select(n => { using var stream = assembly.GetManifestResourceStream(n)!;
                using var reader = new StreamReader(stream); return Parse(reader.ReadToEnd()); }).ToArray();
    }
    private static void Ordinary(string path) { if (!SaveStorage.OrdinaryPath(path)) throw new IOException("Linked mod/settings paths cannot be changed."); }
    private static void UniqueProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in element.EnumerateObject()) {
                if (!keys.Add(property.Name)) throw new InvalidDataException("Duplicate mod fields/settings are not allowed.");
                UniqueProperties(property.Value);
            }
        }
    }
    public static ModDefinition Parse(string content)
    {
        if (Encoding.UTF8.GetByteCount(content) > 65536) throw new InvalidDataException("Mod files must be smaller than 64 KB.");
        content = content.TrimStart('\uFEFF');
        using var document = JsonDocument.Parse(content); UniqueProperties(document.RootElement);
        var mod = JsonSerializer.Deserialize<ModDefinition>(content, Json) ?? throw new InvalidDataException("Empty mod file.");
        if (mod.FormatVersion != 1 || !Regex.IsMatch(mod.Id ?? "", "^[a-z][a-z0-9-]{2,63}$")) throw new InvalidDataException("Unsupported mod version or ID.");
        foreach (var (value, limit) in new[] { (mod.Name, 80), (mod.Author, 80), (mod.Description, 500) })
            if (string.IsNullOrWhiteSpace(value) || value.Length > limit || value.Any(char.IsControl)) throw new InvalidDataException("Mod name, author and description must be plain text.");
        if (mod.Settings == null || mod.Settings.Count is < 1 or > 16) throw new InvalidDataException("A mod needs 1–16 supported settings.");
        foreach (var setting in mod.Settings) Literal(setting.Key, setting.Value);
        return mod;
    }
    private static string Literal(string key, JsonElement value)
    {
        if (key == "hope_pedestrian_style" && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var style) && style is >= 0 and <= 3) return style.ToString();
        var boolean = key is "skate3_frontend_movies_auto_skip" or "skate3_native_render_scene_fog" or
            "skate3_native_render_scene_haze" or "skate3_native_render_scene_shafts" or
            "skate3_native_render_scene_bloom" or "skate3_native_render_scene_ssao";
        if (boolean && (value.ValueKind is JsonValueKind.True or JsonValueKind.False)) return value.GetBoolean() ? "true" : "false";
        if (key == "skate3_field_of_view" && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var fov) && double.IsFinite(fov) && fov is >= 40 and <= 120)
            return fov.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (key == "skate3_native_render_scene_msaa" && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var msaa) && msaa is 1 or 2 or 4 or 8)
            return msaa.ToString();
        throw new InvalidDataException("Unsupported setting or value: " + key);
    }
    private static byte[]? Bytes(string path)
    {
        Ordinary(path);
        if (!File.Exists(path)) return null;
        if (new FileInfo(path).Length > 1048576) throw new IOException("Settings/mod state is too large to update safely.");
        return File.ReadAllBytes(path);
    }
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static void Write(string path, byte[]? bytes)
    {
        Ordinary(path);
        if (bytes == null) { if (File.Exists(path)) File.Delete(path); return; }
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllBytes(temporary, bytes); File.Move(temporary, path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    private void Recover()
    {
        Ordinary(Journal);
        if (!File.Exists(Journal)) return;
        if (gameRunning()) throw new IOException("Close the game to recover an interrupted mod change.");
        if (new FileInfo(Journal).Length > 5000000) throw new IOException("Mod recovery record is too large.");
        var transaction = JsonSerializer.Deserialize<ModTransaction>(File.ReadAllText(Journal), Json) ?? throw new IOException("Unreadable mod recovery record.");
        var before = transaction.BeforeSettings == null ? null : Convert.FromBase64String(transaction.BeforeSettings);
        var beforeState = transaction.BeforeState == null ? null : Convert.FromBase64String(transaction.BeforeState);
        var current = Bytes(Settings); var currentState = Bytes(StatePath);
        bool Same(byte[]? a, byte[]? b) => a == null ? b == null : b != null && a.SequenceEqual(b);
        if (!Same(current, before) && (current == null || Hash(current) != transaction.AfterSettingsHash) ||
            !Same(currentState, beforeState) && (currentState == null || !currentState.SequenceEqual(Convert.FromBase64String(transaction.AfterState))))
            throw new IOException("Settings changed after an interrupted mod update. Recovery copies are in the career's mods folder; automatic restoration stopped.");
        if (!Same(current, before)) Write(Settings, before);
        if (!Same(currentState, beforeState)) Write(StatePath, beforeState);
        File.Delete(Journal);
    }
    private ModState State()
    {
        Recover(); var bytes = Bytes(StatePath);
        var state = bytes == null ? new ModState() : JsonSerializer.Deserialize<ModState>(bytes, Json) ?? throw new IOException("Unreadable mod state.");
        if (state.Enabled == null || state.Originals == null || state.Applied == null) throw new IOException("Unreadable mod state.");
        foreach (var pair in state.Originals.Concat(state.Applied.Select(p => new KeyValuePair<string,string?>(p.Key,p.Value)))) {
            // Ledger lines can only restore their own supported single-line assignment.
            var supported = pair.Key is "hope_pedestrian_style" or "skate3_frontend_movies_auto_skip" or "skate3_field_of_view" or "skate3_native_render_scene_fog" or "skate3_native_render_scene_haze" or "skate3_native_render_scene_shafts" or "skate3_native_render_scene_bloom" or "skate3_native_render_scene_ssao" or "skate3_native_render_scene_msaa";
            if (!supported || pair.Value != null && (pair.Value.Contains('\n') || pair.Value.Contains('\r') || !Regex.IsMatch(pair.Value, "^\\s*" + Regex.Escape(pair.Key) + "\\s*=")))
                throw new IOException("Invalid mod restoration record.");
        }
        return state;
    }
    public List<ModChoice> List()
    {
        var state = State(); Warning = "";
        var catalog = BuiltIns.ToList(); Ordinary(Library);
        if (Directory.Exists(Library)) foreach (var file in Directory.EnumerateFiles(Library, "*.hope-mod.json").Order()) {
            try { var mod = Parse(Encoding.UTF8.GetString(Bytes(file)!)); if (catalog.Any(m => m.Id == mod.Id)) throw new InvalidDataException("Duplicate ID."); catalog.Add(mod); }
            catch (Exception error) when (error is IOException or InvalidDataException or JsonException or ArgumentException) { Warning += Path.GetFileName(file) + ": " + error.Message + " "; }
        }
        if (state.Enabled.Any(id => catalog.All(m => m.Id != id))) Warning += "A previously enabled mod is unavailable. Apply your selection to restore its settings.";
        return catalog.Select(m => new ModChoice { Definition=m, Enabled=state.Enabled.Contains(m.Id) }).ToList();
    }
    public string Import(string path)
    {
        Ordinary(path); var mod = Parse(Encoding.UTF8.GetString(Bytes(path) ?? throw new IOException("Mod file is missing.")));
        Ordinary(Library);
        var target = Path.Combine(Library, mod.Id + ".hope-mod.json"); Ordinary(target);
        if (BuiltIns.Any(m => m.Id == mod.Id) || File.Exists(target)) throw new IOException("A mod with this ID already exists. Existing mods were kept.");
        Directory.CreateDirectory(Library);
        var temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllBytes(temporary, JsonSerializer.SerializeToUtf8Bytes(mod, Json)); File.Move(temporary, target, false); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        return mod.Name;
    }
    private static Dictionary<string,string> Lines(string text)
    {
        var result = new Dictionary<string,string>();
        foreach (var line in text.Split('\n')) {
            var clean = line.TrimEnd('\r'); if (clean.TrimStart().StartsWith('[')) break;
            var match = Regex.Match(clean, "^\\s*([a-zA-Z0-9_]+)\\s*=");
            if (match.Success && !result.TryAdd(match.Groups[1].Value, clean)) throw new IOException("Duplicate settings keys must be repaired before applying mods.");
        }
        return result;
    }
    private static string Patch(string text, Dictionary<string,string?> changes)
    {
        var newline = text.Contains("\r\n") ? "\r\n" : "\n";
        var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
        foreach (var change in changes) {
            var end = lines.FindIndex(l => l.TrimStart().StartsWith('[')); if (end < 0) end = lines.Count;
            var index = lines.FindIndex(0, end, l => Regex.IsMatch(l, "^\\s*" + Regex.Escape(change.Key) + "\\s*="));
            if (index >= 0) { if (change.Value == null) lines.RemoveAt(index); else lines[index] = change.Value; }
            else if (change.Value != null) { if (end == lines.Count && end > 0 && lines[^1] == "") --end; lines.Insert(end, change.Value); }
        }
        return string.Join(newline, lines);
    }
    public string Apply(IEnumerable<string> selected)
    {
        if (gameRunning()) throw new IOException("Close the game before applying mods.");
        var catalog = List(); var state = State();
        var ids = selected.Distinct().Order().ToArray();
        var desired = new Dictionary<string,string>();
        foreach (var id in ids) {
            var mod = catalog.Find(m => m.Definition.Id == id)?.Definition ?? throw new IOException("Selected mod is missing: " + id);
            foreach (var pair in mod.Settings) {
                var line = pair.Key + " = " + Literal(pair.Key, pair.Value);
                if (desired.TryGetValue(pair.Key, out var other) && other != line) throw new IOException("Mods conflict on " + pair.Key + ". Enable one of them.");
                desired[pair.Key] = line;
            }
        }
        var before = Bytes(Settings); var previousState = Bytes(StatePath);
        var text = before == null ? "" : Encoding.UTF8.GetString(before).TrimStart('\uFEFF'); var current = Lines(text);
        var changes = new Dictionary<string,string?>();
        foreach (var key in state.Applied.Keys.Union(desired.Keys)) {
            current.TryGetValue(key, out var line);
            if (!state.Originals.ContainsKey(key) || state.Applied.TryGetValue(key, out var expected) && expected != line) state.Originals[key] = line;
            if (desired.TryGetValue(key, out var next)) changes[key] = next;
            else { changes[key] = state.Originals[key]; state.Originals.Remove(key); }
        }
        state.Enabled = ids; state.Applied = desired;
        var after = Encoding.UTF8.GetBytes((before is { Length: >= 3 } && before[0] == 239 && before[1] == 187 && before[2] == 191 ? "\uFEFF" : "") + Patch(text, changes)); var afterState = JsonSerializer.SerializeToUtf8Bytes(state, Json);
        var transaction = new ModTransaction { BeforeSettings=before == null ? null : Convert.ToBase64String(before), BeforeState=previousState == null ? null : Convert.ToBase64String(previousState), AfterSettingsHash=Hash(after), AfterState=Convert.ToBase64String(afterState) };
        Ordinary(Store); Ordinary(Journal); Directory.CreateDirectory(Store);
        var backups = Path.Combine(Store, "backups", Guid.NewGuid().ToString("N")); Ordinary(backups); Directory.CreateDirectory(backups);
        if (before != null) Write(Path.Combine(backups, "settings.toml"), before);
        if (previousState != null) Write(Path.Combine(backups, "state.json"), previousState);
        Write(Journal, JsonSerializer.SerializeToUtf8Bytes(transaction, Json));
        try { Write(Settings, after); Write(StatePath, afterState); File.Delete(Journal); }
        catch { Recover(); throw; }
        return ids.Length == 0 ? "Mods disabled. Previous values restored; later manual edits were kept." : $"{ids.Length} mods applied to this career. Changes take effect next launch.";
    }
}
