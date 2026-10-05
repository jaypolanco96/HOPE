using System.IO;
using System.Text.Json;

namespace Hope.Launcher;

public sealed record CareerChoice(string? Id, string Label);
public static class Careers
{
    public static bool ValidId(string? id) => id != null && Guid.TryParseExact(id, "N", out _);
    public static string Root(string bundle, string? id) => ValidId(id) ? Path.Combine(bundle, "careers", id!) : bundle;
    public static List<CareerChoice> List(string bundle)
    {
        var choices = new List<CareerChoice> { new(null, "Main career") };
        var parent = Path.Combine(bundle, "careers");
        if (!Directory.Exists(parent) || !SaveStorage.OrdinaryPath(parent)) return choices;
        foreach (var directory in Directory.EnumerateDirectories(parent).Order())
        {
            var id = Path.GetFileName(directory);
            if (!ValidId(id) || !SaveStorage.OrdinaryPath(directory) || !File.Exists(Path.Combine(directory, "career.ready"))) continue;
            choices.Add(new(id, "New career · " + Directory.GetCreationTime(directory).ToString("MMM d, HH:mm")));
        }
        return choices;
    }
    public static string Create(LauncherState state)
    {
        if (state.IsGameRunning()) throw new IOException("Close the game before creating a career.");
        if (!state.HasGame) throw new IOException("Install your own game in Game setup first.");
        var id = Guid.NewGuid().ToString("N");
        var root = Root(state.BundleRoot, id);
        if (!SaveStorage.OrdinaryPath(root)) throw new IOException("The careers folder uses a linked path. Choose a regular installation folder.");
        // A folder becomes selectable only after all preparation succeeds.
        Directory.CreateDirectory(root);
        File.Copy(Path.Combine(state.BundleRoot, "skate3.exe"), Path.Combine(root, "skate3.exe"));
        var runtimeFiles = Directory.GetFiles(state.BundleRoot, "rexruntime*.dll");
        if (runtimeFiles.Length == 0) throw new IOException("The matching game runtime is missing. Repair this installation before creating a career.");
        foreach (var runtime in runtimeFiles) File.Copy(runtime, Path.Combine(root, Path.GetFileName(runtime)));
        if (File.Exists(state.SettingsPath)) File.Copy(state.SettingsPath, Path.Combine(root, "settings.toml"));
        SettingsFile.AtomicWrite(Path.Combine(root, "skate3.toml"), "game_data_root = " + JsonSerializer.Serialize(state.GameRoot) + "\n");
        File.WriteAllText(Path.Combine(root, "portable.txt"), "");
        Directory.CreateDirectory(Path.Combine(root, "saves"));
        // Deliberately do not copy career packages or profile metadata.
        File.WriteAllText(Path.Combine(root, "career.ready"), "HOPE isolated career\n");
        return id;
    }
}
