using System.IO;

namespace Hope.Launcher;

internal static class QoLTests
{
    public static void Run(string root, Action<bool, string> check)
    {
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "portable.txt"), "");
        var settings = Path.Combine(root, "settings.toml");
        var career = Path.Combine(root, "saves", "B13E000000000001", "ALIAS_SKATER", "SKATER.P");
        Directory.CreateDirectory(Path.GetDirectoryName(career)!);
        File.WriteAllText(career, "career progress sentinel");
        var profiles = Path.Combine(root, "profiles.toml");
        File.WriteAllText(profiles, "profile sentinel");
        File.WriteAllText(settings, "fullscreen = false\nvsync = true\nshow_fps_counter = true\nunrelated = 77\n");
        var original = File.ReadAllBytes(settings);
        using (var locked = new FileStream(settings, FileMode.Open, FileAccess.Read, FileShare.None))
        using (var window = new MainWindow(root))
        {
            window.Navigate("Help");
            check(window.HelpPanel.Visibility == System.Windows.Visibility.Visible && window.Feedback.Text.Contains("Help & recovery"), "Unreadable settings still allow recovery page and warning");
        }
        using (var window = new MainWindow(root))
        {
            check(window.FullscreenCheck.IsChecked == false && window.VsyncCheck.IsChecked == true && window.FpsCheck.IsChecked == true, "Display/FPS preferences load into visible controls");
            window.Navigate("Help");
            check(window.HelpPanel.Visibility == System.Windows.Visibility.Visible, "Recovery page accessible without starting game");
        }
        var backup = SettingsRecovery.Reset(settings)!;
        check(!File.Exists(settings) && File.ReadAllBytes(backup).SequenceEqual(original), "Reset retains exact settings bytes in backup");
        check(SettingsRecovery.List(settings).Count == 1, "Reset backup available for selection");
        check(SettingsRecovery.Reset(settings) == null, "Repeated reset leaves defaults unchanged");
        check(File.ReadAllText(career) == "career progress sentinel" && File.ReadAllText(profiles) == "profile sentinel", "Reset preserves career and profile bytes");
        SettingsRecovery.Restore(settings, backup);
        check(File.ReadAllBytes(settings).SequenceEqual(original), "Restore recovers exact settings bytes");
        File.WriteAllText(settings, "fullscreen = true\ncustom = 55\n");
        SettingsRecovery.Restore(settings, backup);
        check(SettingsRecovery.List(settings).Any(item => File.ReadAllText(item.Path).Contains("custom = 55")), "Restore archives current settings before replacement");
        var other = Path.Combine(root, "other-career", "settings.toml");
        Directory.CreateDirectory(Path.GetDirectoryName(other)!);
        File.WriteAllText(other, "another career");
        var otherBackup = SettingsRecovery.Reset(other)!;
        bool rejected = false;
        try { SettingsRecovery.Restore(settings, otherBackup); } catch (Exception error) when (error is IOException or UnauthorizedAccessException) { rejected = true; }
        check(rejected && File.ReadAllBytes(settings).SequenceEqual(original), "Cross-career backup rejected without changing target");
        using (var locked = new FileStream(settings, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            rejected = false;
            try { SettingsRecovery.Reset(settings); } catch (Exception error) when (error is IOException or UnauthorizedAccessException) { rejected = true; }
            check(rejected && File.ReadAllBytes(settings).SequenceEqual(original), "Locked reset preserves live settings");
            rejected = false;
            try { SettingsRecovery.Restore(settings, backup); } catch (Exception error) when (error is IOException or UnauthorizedAccessException) { rejected = true; }
            check(rejected && File.ReadAllBytes(settings).SequenceEqual(original), "Failed restore preserves live settings");
        }
        check(Directory.GetFiles(root, "*.restore-tmp").Length == 0, "Failed restore leaves no staged files");
        check(File.ReadAllText(career) == "career progress sentinel" && File.ReadAllText(profiles) == "profile sentinel", "Recovery failures preserve progress and metadata");
        var edges = new ControllerEdges();
        check(edges.Update(0x1000, 0, 0) == 0, "Held A on first connection cannot activate");
        check(edges.Update(0x1000, 1, 0) == 0, "Held A does not repeat");
        edges.Update(0, 2, 0);
        check(edges.Update(0x1000, 3, 0) == 0x1000, "Fresh A press activates once");
        check(edges.Update(0x1000, 4, null) == 0, "Disconnect generates no activation");
        check(edges.Update(0x1000, 5, 1) == 0, "Held A on reconnect cannot activate");
        check(edges.Update(0x1000, 6, 2) == 0, "Controller handoff cannot activate held A");
        edges.Update(0, 7, 2);
        check(edges.Update(2, 9, 2) == 2, "D-pad fresh press moves focus");
        check(edges.Update(2, 428, 2) == 0 && edges.Update(2, 429, 2) == 2, "D-pad repeat starts after 420ms");
        check(edges.Update(2, 538, 2) == 0 && edges.Update(2, 539, 2) == 2, "D-pad repeat interval is 110ms");
    }
}
