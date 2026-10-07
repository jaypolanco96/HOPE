using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace Hope.Launcher;

internal static class UpdateTests
{
    public static void Run(string root, Action<bool, string> check)
    {
        Directory.CreateDirectory(root);
        string Metadata(string tag = "hope-v0.6.7", bool prerelease = false, string host = "github.com", string? digest = null) =>
            JsonSerializer.Serialize(new {
                tag_name = tag, draft = false, prerelease,
                assets = new[] { new { name = "HOPE-0.6.7-windows-x64.zip", size = 500,
                    browser_download_url = $"https://{host}/jaypolanco96/HOPE/releases/download/{tag}/HOPE-0.6.7-windows-x64.zip",
                    digest = digest ?? "sha256:" + new string('a', 64) } }
            });
        check(LauncherUpdates.Parse(Metadata()).Version == new Version(0, 6, 7), "Update version parsed");
        check(LauncherUpdates.Parse(Metadata("hope-v0.6.5-preview.1")).Version == new Version(0, 6, 5), "Published legacy release tag accepted");
        foreach (var json in new[] { Metadata(prerelease: true), Metadata(host: "example.com"), Metadata(digest: "missing"), Metadata(tag: "bad") }) {
            bool rejected = false;
            try { LauncherUpdates.Parse(json); } catch (IOException) { rejected = true; }
            check(rejected, "Invalid release metadata refused");
        }
        foreach (var name in new[] { "settings.toml", "hope-launcher.json", "skate3.toml", "game/default.xex", "../HOPE.dll", "careers/id/skate3.exe", "mods/state.json", "C:/HOPE.exe" })
            check(!LauncherUpdates.IsProgram(name), "Update excludes user/game/traversal files");
        check(LauncherUpdates.IsProgram("fr/System.Windows.resources.dll"), "Runtime locale file accepted");
        var archive = Path.Combine(root, "release.zip");
        using (var zip = ZipFile.Open(archive, ZipArchiveMode.Create)) {
            foreach (var name in new[] { "HOPE.exe", "HOPE.dll", "HOPE.deps.json", "HOPE.runtimeconfig.json", "skate3.exe", "rexruntimerd.dll", "coreclr.dll", "fr/System.Windows.resources.dll", "settings.toml", "skate3.toml", "hope-launcher.json", "game/default.xex", "../escape.dll" }) {
                using var writer = new StreamWriter(zip.CreateEntry("HOPE-0.6.7-windows-x64/" + name).Open());
                writer.Write("fixture " + name);
            }
        }
        var stage = Path.Combine(root, "stage");
        LauncherUpdates.ExtractPrograms(archive, stage);
        check(File.Exists(Path.Combine(stage, "payload", "coreclr.dll")), "Bundled runtime extracted");
        check(File.Exists(Path.Combine(stage, "payload", "fr", "System.Windows.resources.dll")), "Runtime locale extracted");
        check(!File.Exists(Path.Combine(stage, "payload", "settings.toml")), "Archive settings not staged");
        check(!File.Exists(Path.Combine(stage, "escape.dll")), "Archive traversal not extracted");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(stage, "payload.json")));
        check(manifest.RootElement.GetProperty("files").GetArrayLength() == 8, "Only allowed program files in manifest");
    }
}
