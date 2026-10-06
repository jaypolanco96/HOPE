using System.IO;
using System.Text.Json;
using System.Windows;

namespace Hope.Launcher;

internal static class BugfixTests
{
    public static void Run(string root, Action<bool, string> check)
    {
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "portable.txt"), "");
        File.WriteAllText(Path.Combine(root, "skate3.exe"), "fixture; never executed");
        File.WriteAllText(Path.Combine(root, "rexruntime.dll"), "fixture; never loaded");
        var settings = Path.Combine(root, "settings.toml");
        File.WriteAllText(settings, "vsync = true\n");
        var backup = SettingsRecovery.Reset(settings)!;
        SettingsRecovery.Restore(settings, backup);
        Directory.CreateDirectory(Path.Combine(root, "game"));
        File.WriteAllText(Path.Combine(root, "game", "default.xex"), "fixture");
        var state = new LauncherState(root);
        var career = Careers.Create(state);
        var mainSave = Path.Combine(root, "saves", "B13E000000000001", "MAIN_SAVE");
        var freshSave = Path.Combine(Careers.Root(root, career), "saves", "B13E000000000002", "FRESH_SAVE");
        Directory.CreateDirectory(mainSave); Directory.CreateDirectory(freshSave);
        File.WriteAllText(Path.Combine(mainSave, "SKATER.P"), "main save sentinel");
        File.WriteAllText(Path.Combine(freshSave, "SKATER.P"), "fresh save sentinel");
        using (var window = new MainWindow(root))
        {
            window.Navigate("Saves");
            check(window.SaveList.SelectedItem is SaveEntry { Name: "MAIN_SAVE" }, "Main-career saves initially shown");
            window.DeleteConfirm.Visibility = Visibility.Visible;
            window.CareerCombo.SelectedIndex = 1;
            check(window.SaveList.SelectedItem is SaveEntry { Name: "FRESH_SAVE" }, "Career switch rebuilds saves list immediately");
            check(window.DeleteConfirm.Visibility == Visibility.Collapsed, "Career switch clears old save confirmation");
            window.CareerCombo.SelectedIndex = 0;
            window.Navigate("Help");
            check(window.BackupCombo.Items.Count > 0, "Main-career settings backups shown");
            window.CareerCombo.SelectedIndex = 1;
            check(window.BackupCombo.Items.Count == 0 && window.BackupStatus.Visibility == Visibility.Visible, "Career switch replaces recovery list with selected-career backups");
            check(!window.RestoreSettingsButton.IsEnabled, "Restore disabled when selected career has no backup");
            window.CareerCombo.SelectedIndex = 0;
            check(window.BackupCombo.Items.Count > 0 && window.RestoreSettingsButton.IsEnabled, "Switching back restores main-career recovery controls");
        }
        check(File.ReadAllText(Path.Combine(mainSave, "SKATER.P")) == "main save sentinel" &&
            File.ReadAllText(Path.Combine(freshSave, "SKATER.P")) == "fresh save sentinel", "Career page refresh leaves both save packages untouched");
        var preferences = Path.Combine(root, "hope-launcher.json");
        var originalPreferences = File.ReadAllBytes(preferences);
        using (var locked = new FileStream(preferences, FileMode.Open, FileAccess.Read, FileShare.None))
        using (var window = new MainWindow(root))
        {
            window.Navigate("Setup");
            check(window.SetupPanel.Visibility == Visibility.Visible, "Locked launcher preferences do not prevent setup access");
            check(new LauncherState(root).PreferencesWarning?.Contains("unavailable") == true, "Locked preferences produce recovery guidance");
        }
        check(File.ReadAllBytes(preferences).SequenceEqual(originalPreferences), "Locked preferences are preserved without an automatic reset");
        var config = Path.Combine(root, "skate3.toml");
        foreach (var value in new[] { "\"unfinished", "\"\"", "' '", "\"bad\\u0000path\"" })
        {
            File.WriteAllText(config, "game_data_root = " + value + "\n");
            state = new LauncherState(root);
            check(!state.HasGame && state.GameFolderWarning?.Contains("Game setup") == true, "Invalid game folder reported without throwing from readiness check");
            bool rejected = false;
            try { state.BuildStartInfo(); } catch (IOException) { rejected = true; }
            check(rejected, "Invalid game folder cannot silently launch a fallback installation");
            using var window = new MainWindow(root);
            window.Navigate("Setup");
            check(window.SetupPanel.Visibility == Visibility.Visible && window.ReadyDetail.Text.Contains("choose your installed"), "Invalid game folder leaves repair interface accessible");
        }
        SettingsFile.Update(config, new Dictionary<string, string> { ["game_data_root"] = JsonSerializer.Serialize(Path.Combine(root, "game")) });
        check(new LauncherState(root).HasGame, "Choosing installed folder repairs the invalid root setting");
        using (var locked = new FileStream(config, FileMode.Open, FileAccess.Read, FileShare.None))
            check(!new LauncherState(root).HasGame && new LauncherState(root).GameFolderWarning != null, "Locked game config reports unavailable installation safely");
    }
}
