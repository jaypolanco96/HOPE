using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Hope.Launcher;

public sealed class LauncherPreferences
{
    public string? IsoPath { get; set; }
    public string? PhotoPath { get; set; }
    public int PhotoIndex { get; set; }
}

public static class SettingsFile
{
    public static string Read(string path, string key, string fallback)
    {
        if (!File.Exists(path)) return fallback;
        foreach (var line in File.ReadLines(path))
        {
            if (line.TrimStart().StartsWith('[')) break;
            var match = Regex.Match(line, @"^\s*" + Regex.Escape(key) + @"\s*=\s*(.*)$");
            if (match.Success)
            {
                var value = match.Groups[1].Value;
                char quote = '\0';
                bool escaped = false;
                for (int i = 0; i < value.Length; i++)
                {
                    char c = value[i];
                    if (escaped) { escaped = false; continue; }
                    if (quote == '"' && c == '\\') { escaped = true; continue; }
                    if (quote != '\0') { if (c == quote) quote = '\0'; continue; }
                    if (c == '"' || c == '\'') quote = c;
                    else if (c == '#') return value[..i].Trim();
                }
                return value.Trim();
            }
        }
        return fallback;
    }

    public static string StringValue(string value)
    {
        if (value.StartsWith('\'') && value.EndsWith('\'')) return value[1..^1];
        if (value.StartsWith('"')) return JsonSerializer.Deserialize<string>(value) ?? "";
        return value;
    }

    public static void Update(string path, IReadOnlyDictionary<string, string> updates)
    {
        var lines = File.Exists(path) ? File.ReadAllLines(path).ToList() : [];
        var remaining = new Dictionary<string, string>(updates);
        int rootEnd = lines.FindIndex(line => line.TrimStart().StartsWith('['));
        if (rootEnd < 0) rootEnd = lines.Count;
        for (int i = 0; i < rootEnd; i++)
        {
            var match = Regex.Match(lines[i], @"^\s*([A-Za-z0-9_]+)\s*=");
            if (match.Success && remaining.Remove(match.Groups[1].Value, out var value))
                lines[i] = match.Groups[1].Value + " = " + value;
        }
        lines.InsertRange(rootEnd, remaining.Select(pair => pair.Key + " = " + pair.Value));
        AtomicWrite(path, string.Join(Environment.NewLine, lines) + Environment.NewLine);
    }

    public static void AtomicWrite(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, content, new UTF8Encoding(false));
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}

public sealed record SaveEntry(string Name, string PackageRoot, string HeaderRoot, string Label)
{
    public string PackagePath => Path.Combine(PackageRoot, Name);
}

public static class SaveStorage
{
    public static bool SafeName(string name) => Regex.IsMatch(name, @"^[A-Za-z0-9_ -]+$") && name != "Headers";

    public static bool OrdinaryPath(string path)
    {
        for (var current = Path.GetFullPath(path); !string.IsNullOrEmpty(current);
             current = Path.GetDirectoryName(current))
        {
            try { if (File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint)) return false; }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
        return true;
    }

    public static List<SaveEntry> List(string userRoot, string bundleRoot)
    {
        var portable = Path.Combine(bundleRoot, "saves");
        var flattened = Directory.Exists(portable);
        var parent = flattened ? portable : userRoot;
        var result = new List<SaveEntry>();
        if (!Directory.Exists(parent) || !OrdinaryPath(parent)) return result;
        foreach (var profile in Directory.EnumerateDirectories(parent).Order())
        {
            var xuid = Path.GetFileName(profile);
            if (!Regex.IsMatch(xuid, "^[A-Fa-f0-9]{16}$") || !OrdinaryPath(profile)) continue;
            var packages = flattened ? profile : Path.Combine(profile, "454108E6", "00000001");
            var headers = flattened ? Path.Combine(profile, "Headers") :
                Path.Combine(profile, "454108E6", "Headers", "00000001");
            if (!Directory.Exists(packages) || !OrdinaryPath(packages)) continue;
            foreach (var package in Directory.EnumerateDirectories(packages).Order())
            {
                var name = Path.GetFileName(package);
                if (!SafeName(name) || !OrdinaryPath(package)) continue;
                var label = name == "ALIAS_SKATER" ? "Career" : name;
                result.Add(new(name, packages, headers, $"{label}  ·  Profile {xuid[^6..]}"));
            }
        }
        return result;
    }

    // Caller must establish that the game is closed. Rename fails rather than
    // copying/removing an open save; metadata failure rolls the package back.
    public static string Archive(SaveEntry entry)
    {
        var header = Path.Combine(entry.HeaderRoot, entry.Name + ".header");
        var recoveryBase = Path.Combine(Path.GetDirectoryName(entry.PackageRoot)!,
            ".pc-save-backups", Path.GetFileName(entry.PackageRoot));
        if (!SafeName(entry.Name) || !Directory.Exists(entry.PackagePath) ||
            !OrdinaryPath(entry.PackagePath) || !OrdinaryPath(entry.HeaderRoot) ||
            !OrdinaryPath(recoveryBase) || !OrdinaryPath(header) || Directory.Exists(header))
            throw new IOException("This save is missing, inaccessible, or uses a linked folder. Nothing was removed.");
        // Enumerate without following reparse points, including nested junctions.
        var pending = new Stack<string>();
        pending.Push(entry.PackagePath);
        while (pending.Count > 0)
        {
            foreach (var item in Directory.EnumerateFileSystemEntries(pending.Pop()))
            {
                var attributes = File.GetAttributes(item);
                if (attributes.HasFlag(FileAttributes.ReparsePoint))
                    throw new IOException("This save contains a linked file or folder. Nothing was removed.");
                if (attributes.HasFlag(FileAttributes.Directory)) pending.Push(item);
            }
        }
        var recovery = Path.Combine(recoveryBase, entry.Name + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") +
            "-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(recovery);
        File.WriteAllText(Path.Combine(recovery, "RESTORE.txt"),
            $"Close HOPE before restoring.\nMove data to: {entry.PackagePath}\n" +
            $"Move content.header, if present, to: {header}\nPreserve any new save before restoring this copy.\n");
        Directory.Move(entry.PackagePath, Path.Combine(recovery, "data"));
        try
        {
            if (File.Exists(header)) File.Move(header, Path.Combine(recovery, "content.header"));
        }
        catch (Exception metadataError)
        {
            try { Directory.Move(Path.Combine(recovery, "data"), entry.PackagePath); }
            catch (Exception rollbackError)
            {
                throw new IOException($"Removal stopped. Your data remains at {recovery}. " +
                    rollbackError.Message, metadataError);
            }
            throw new IOException("Removal stopped; your save was restored to its original location. " +
                metadataError.Message, metadataError);
        }
        return recovery;
    }
}

public sealed class LauncherState
{
    public string BundleRoot { get; }
    public string UserRoot => File.Exists(Path.Combine(BundleRoot, "portable.txt")) ? BundleRoot :
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "skate3");
    public string SettingsPath => Path.Combine(UserRoot, "settings.toml");
    public string ConfigPath => Path.Combine(BundleRoot, "skate3.toml");
    public string GameExecutable => Path.Combine(BundleRoot, "skate3.exe");
    public LauncherPreferences Preferences { get; private set; } = new();
    public string? PreferencesWarning { get; private set; }
    public string GameRoot => Path.GetFullPath(SettingsFile.StringValue(
        SettingsFile.Read(ConfigPath, "game_data_root", JsonSerializer.Serialize(Path.Combine(BundleRoot, "game")))), BundleRoot);
    public bool HasGame => File.Exists(Path.Combine(GameRoot, "default.xex"));
    public bool HasIso => !string.IsNullOrWhiteSpace(Preferences.IsoPath) && File.Exists(Preferences.IsoPath);

    public LauncherState(string root)
    {
        BundleRoot = Path.GetFullPath(root);
        var path = Path.Combine(root, "hope-launcher.json");
        if (File.Exists(path))
        {
            try { Preferences = JsonSerializer.Deserialize<LauncherPreferences>(File.ReadAllText(path)) ?? new(); }
            catch (JsonException) { PreferencesWarning = "Launcher preferences could not be read. Game settings and saves are intact."; }
        }
    }

    public void SavePreferences() => SettingsFile.AtomicWrite(Path.Combine(BundleRoot, "hope-launcher.json"),
        JsonSerializer.Serialize(Preferences, new JsonSerializerOptions { WriteIndented = true }));

    public bool IsGameRunning()
    {
        foreach (var process in Process.GetProcessesByName("skate3"))
        {
            using (process)
            {
                try
                {
                    // Multiple installations may share the same save location.
                    // Block maintenance while any Skate 3 process is alive.
                    if (!process.HasExited) return true;
                }
                catch { return true; } // Do not edit saves/settings when ownership cannot be established.
            }
        }
        return false;
    }

    public ProcessStartInfo BuildStartInfo()
    {
        if (!File.Exists(GameExecutable)) throw new FileNotFoundException("The HOPE game executable is missing. Keep HOPE.exe and skate3.exe in the same folder.");
        if (!HasGame && !HasIso) throw new InvalidOperationException("Choose your own Skate 3 Xbox 360 ISO in Game setup first.");
        var info = new ProcessStartInfo(GameExecutable) { WorkingDirectory = BundleRoot, UseShellExecute = false };
        // Never inherit an unrelated automated installer/demo path from the host.
        info.Environment.Remove("SKATE3_INSTALL_ISO");
        info.Environment.Remove("SKATE3_INSTALL_TU");
        if (!HasGame && HasIso) info.Environment["SKATE3_INSTALL_ISO"] = Preferences.IsoPath!;
        return info;
    }
}
