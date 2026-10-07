using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Hope.Launcher;

internal static class SelfTests
{
    public static string Run(string? linkFixture = null)
    {
        var root = Path.Combine(Path.GetTempPath(), "hope-launcher-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        int checks = 0;
        void Check(bool condition, string message) { checks++; if (!condition) throw new InvalidOperationException(message); }
        void Write(string path, string content) { Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, content); }
        try
        {
            var settings = Path.Combine(root, "settings.toml");
            Write(settings, "unrelated = 42\nskate3_native_render_scene_fog = true # old\n[other]\nvalue = 99\n");
            SettingsFile.Update(settings, new Dictionary<string, string> { ["skate3_native_render_scene_fog"] = "false", ["skate3_native_render_scene_msaa"] = "8" });
            Check(SettingsFile.Read(settings, "skate3_native_render_scene_fog", "") == "false", "Fog persists");
            Check(SettingsFile.Read(settings, "skate3_native_render_scene_msaa", "") == "8", "MSAA inserted at root");
            Check(File.ReadAllText(settings).Contains("unrelated = 42"), "Unrelated setting retained");
            Check(File.ReadAllText(settings).Contains("[other]\r\nvalue = 99"), "Other table retained");
            var quotedPath = Path.Combine(root, "my game #1 'street'");
            SettingsFile.Update(settings, new Dictionary<string, string> { ["game_data_root"] = JsonSerializer.Serialize(quotedPath) });
            Check(SettingsFile.StringValue(SettingsFile.Read(settings, "game_data_root", "")) == quotedPath, "Quoted path and hash character preserved");
            File.WriteAllText(Path.Combine(root, "portable.txt"), "");
            var badPhoto = Path.Combine(root, "assets", "gameplay", "broken.jpg");
            Write(badPhoto, "damaged optional photo");
            using (var window = new MainWindow(root))
            {
                Check(window.Backdrop.Source == null, "Damaged optional photo does not block launcher startup");
                Check(window.Feedback.Text.Contains("background image"), "Damaged photo shows recovery advice");
                window.Navigate("Graphics");
                Check(window.GraphicsPanel.Visibility == System.Windows.Visibility.Visible, "Settings remain available without artwork");
            }
            Check(File.ReadAllText(badPhoto) == "damaged optional photo", "Damaged photo retained for recovery");
            var packages = Path.Combine(root, "B13E000000000001", "454108E6", "00000001");
            var headers = Path.Combine(root, "B13E000000000001", "454108E6", "Headers", "00000001");
            Write(Path.Combine(packages, "ALIAS_SKATER", "SKATER.P"), "current career");
            Write(Path.Combine(headers, "ALIAS_SKATER.header"), "metadata");
            Write(Path.Combine(packages, "OTHER_SAVE", "other.dat"), "other save");
            var list = SaveStorage.List(root, root);
            Check(list.Count == 2, "Standard storage lists both saves");
            var selected = list.Single(item => item.Name == "ALIAS_SKATER");
            var recovery = SaveStorage.Archive(selected);
            Check(!Directory.Exists(selected.PackagePath), "Selected save removed from active storage");
            Check(File.ReadAllText(Path.Combine(recovery, "data", "SKATER.P")) == "current career", "Recovery data retained");
            Check(File.ReadAllText(Path.Combine(recovery, "content.header")) == "metadata", "Metadata retained");
            Check(File.Exists(Path.Combine(recovery, "RESTORE.txt")), "Restore instructions present");
            Check(File.ReadAllText(Path.Combine(packages, "OTHER_SAVE", "other.dat")) == "other save", "Other save retained");
            Check(SaveStorage.List(root, root).Count == 1, "Recovery directory not an active save");
            bool rejected = false;
            try { SaveStorage.Archive(selected with { Name = "../OTHER_SAVE" }); } catch (IOException) { rejected = true; }
            Check(rejected, "Traversal rejected");
            var locked = new SaveEntry("LOCKED", packages, headers, "Locked fixture");
            Write(Path.Combine(locked.PackagePath, "SKATER.P"), "keep");
            Write(Path.Combine(headers, "LOCKED.header"), "keep header");
            using (var handle = new FileStream(Path.Combine(headers, "LOCKED.header"), FileMode.Open, FileAccess.Read, FileShare.None))
            {
                rejected = false;
                try { SaveStorage.Archive(locked); } catch (IOException) { rejected = true; }
                Check(rejected, "Locked metadata rejects removal");
                Check(File.ReadAllText(Path.Combine(locked.PackagePath, "SKATER.P")) == "keep", "Metadata rollback restores package");
            }
            var portable = Path.Combine(root, "saves", "B13E000000000002");
            Write(Path.Combine(portable, "ALIAS_SKATER", "SKATER.P"), "portable");
            Write(Path.Combine(portable, "Headers", "ALIAS_SKATER.header"), "portable metadata");
            list = SaveStorage.List(root, root);
            Check(list.Count == 1 && list[0].PackageRoot == portable, "Portable override wins and Headers excluded");
            recovery = SaveStorage.Archive(list[0]);
            Check(File.ReadAllText(Path.Combine(recovery, "content.header")) == "portable metadata", "Portable header retained");
            var state = new LauncherState(root);
            Check(state.UserRoot == root, "Portable user data scope");
            Write(state.GameExecutable, "not an executable; never launched in this test");
            rejected = false;
            try { state.BuildStartInfo(); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "No ISO/game blocks Play");
            var iso = Path.Combine(root, "my own game.iso");
            Write(iso, "fixture");
            state.Preferences.IsoPath = iso;
            state.SavePreferences();
            state = new LauncherState(root);
            var start = state.BuildStartInfo();
            Check(start.Environment["SKATE3_INSTALL_ISO"] == iso && !start.UseShellExecute, "ISO passed safely to installer without shell");
            Check(start.WorkingDirectory == root && start.FileName == state.GameExecutable, "Launch targets correct bundle");
            Write(Path.Combine(state.GameRoot, "default.xex"), "fixture");
            Check(state.HasGame, "Existing extracted game recognized");
            start = state.BuildStartInfo();
            Check(!start.Environment.ContainsKey("SKATE3_INSTALL_ISO"), "Existing game does not reimport ISO");
            Check(!start.Environment.ContainsKey("SKATE3_INSTALL_TU"), "Inherited installer override removed");
            Check(start.Environment["HOPE_HOME_LAUNCHER"] == Path.Combine(root, "HOPE.exe"), "Return Home targets parent launcher");
            Write(Path.Combine(root, "rexruntimerd.dll"), "runtime fixture");
            var originalSettings = File.ReadAllText(state.SettingsPath);
            var gameRoot = state.GameRoot;
            var career = Careers.Create(state);
            Check(Careers.List(root).Count == 2, "Completed career listed alongside main career");
            state.SelectCareer(career);
            var freshRoot = state.ActiveRoot;
            Check(freshRoot != root && state.UserRoot == freshRoot, "Fresh career has isolated portable data root");
            Check(state.GameRoot == gameRoot && state.HasGame, "Fresh career shares supplied game data");
            Check(File.ReadAllText(state.SettingsPath) == originalSettings, "Fresh career copies graphics preferences");
            Check(SaveStorage.List(state.UserRoot, state.ActiveRoot).Count == 0, "Fresh career starts without existing saves");
            Check(!File.Exists(Path.Combine(freshRoot, "profiles", "profiles.toml")), "Fresh profile created by game without copied metadata");
            start = state.BuildStartInfo();
            Check(start.WorkingDirectory == freshRoot && start.FileName == Path.Combine(freshRoot, "skate3.exe"), "Fresh launch uses isolated program and scope");
            Check(start.Environment["HOPE_HOME_LAUNCHER"] == Path.Combine(root, "HOPE.exe"), "Fresh session returns to original launcher");
            state = new LauncherState(root);
            Check(state.ActiveRoot == freshRoot, "Career selection survives launcher restart");
            Write(Path.Combine(freshRoot, "saves", "B13E000000000001", "ALIAS_SKATER", "SKATER.P"), "new career fixture");
            Check(SaveStorage.List(state.UserRoot, state.ActiveRoot).Count == 1, "New career save visible only in selected scope");
            state.SelectCareer(null);
            Check(state.ActiveRoot == root && File.ReadAllText(state.SettingsPath) == originalSettings, "Main career and settings restored on switch");
            Check(File.ReadAllText(Path.Combine(packages, "OTHER_SAVE", "other.dat")) == "other save", "New career never alters prior packages");
            Check(File.ReadAllText(Path.Combine(freshRoot, "saves", "B13E000000000001", "ALIAS_SKATER", "SKATER.P")) == "new career fixture", "Switching away retains fresh progress");
            rejected = false;
            try { state.SelectCareer("../outside"); } catch (IOException) { rejected = true; }
            Check(rejected && state.ActiveRoot == root, "Unsafe career selection rejected without changing scope");
            Directory.CreateDirectory(Path.Combine(root, "careers", Guid.NewGuid().ToString("N")));
            Check(Careers.List(root).Count == 2, "Incomplete career preparation not selectable");
            state.Preferences.CareerId = Guid.NewGuid().ToString("N");
            state.SavePreferences();
            state = new LauncherState(root);
            Check(state.ActiveRoot == root && state.PreferencesWarning != null, "Missing selected career recovers to main scope");
            Write(Path.Combine(root, "hope-launcher.json"), "{broken");
            state = new LauncherState(root);
            Check(state.PreferencesWarning != null && state.Preferences.IsoPath == null, "Corrupt launcher preferences recover");
            Check(File.ReadAllText(Path.Combine(packages, "OTHER_SAVE", "other.dat")) == "other save", "Preferences recovery preserves saves");
            if (linkFixture is not null)
            {
                Check(!SaveStorage.OrdinaryPath(Path.Combine(linkFixture, "packages", "LINKED_SAVE")), "Junction package rejected");
                foreach (var name in new[] { "LINKED_SAVE", "ORDINARY_SAVE" })
                {
                    rejected = false;
                    try { SaveStorage.Archive(new(name, Path.Combine(linkFixture, "packages"), Path.Combine(linkFixture, "headers"), name)); }
                    catch (IOException) { rejected = true; }
                    Check(rejected, "Junction package or nested contents cannot be archived");
                }
                Check(File.ReadAllText(Path.Combine(linkFixture, "outside", "SKATER.P")) == "outside fixture", "Junction target preserved");
                rejected = false;
                try { SettingsRecovery.Reset(Path.Combine(linkFixture, "packages", "LINKED_SAVE", "SKATER.P")); }
                catch (IOException) { rejected = true; }
                Check(rejected && File.ReadAllText(Path.Combine(linkFixture, "outside", "SKATER.P")) == "outside fixture", "Settings reset rejects linked source without modifying target");
            }
            ModTests.Run(Path.Combine(root, "mods-tests"), Check);
            AdvancedGraphicsTests.Run(Path.Combine(root, "advanced-graphics"), Check);
            QoLTests.Run(Path.Combine(root, "qol"), Check);
            DlcTests.Run(Path.Combine(root, "dlc-tests"), Check, linkFixture);
            BugfixTests.Run(Path.Combine(root, "bugfix-tests"), Check);
            UpdateTests.Run(Path.Combine(root, "update-tests"), Check);
            return $"Passed {checks} HOPE launcher fixture checks. No game was launched; no real saves were modified.\n";
        }
        finally
        {
            // Fixed GUID fixture directly under the OS temp directory.
            if (Path.GetDirectoryName(root) == Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar))
                Directory.Delete(root, true);
        }
    }
}
