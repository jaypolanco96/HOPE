using System.IO;

namespace Hope.Launcher;

public sealed record SettingsBackup(string Path, string Label);
public static class SettingsRecovery
{
    private static string Parent(string path) => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path))!, ".settings-backups");
    public static List<SettingsBackup> List(string path)
    {
        var parent = Parent(path);
        if (!Directory.Exists(parent) || !SaveStorage.OrdinaryPath(parent)) return [];
        return Directory.EnumerateDirectories(parent).Where(SaveStorage.OrdinaryPath)
            .Select(directory => new SettingsBackup(System.IO.Path.Combine(directory, "settings.toml"), System.IO.Path.GetFileName(directory)))
            .Where(backup => File.Exists(backup.Path) && SaveStorage.OrdinaryPath(backup.Path))
            .OrderByDescending(backup => backup.Label).ToList();
    }
    private static string Prepare(string path)
    {
        if (!SaveStorage.OrdinaryPath(path) || !SaveStorage.OrdinaryPath(Parent(path)))
            throw new IOException("Settings recovery requires a regular folder, without linked files or folders.");
        var directory = System.IO.Path.Combine(Parent(path), DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff") + "-" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(directory);
        File.WriteAllText(System.IO.Path.Combine(directory, "RESTORE.txt"),
            "Close the game before restoring. Copy settings.toml to: " + System.IO.Path.GetFullPath(path) + "\nCareer saves and profile metadata are not part of this backup.\n");
        return System.IO.Path.Combine(directory, "settings.toml");
    }
    public static string? Reset(string path)
    {
        if (!File.Exists(path)) return null;
        var backup = Prepare(path);
        File.Move(path, backup);
        return backup;
    }
    public static void Restore(string path, string backup)
    {
        if (!SaveStorage.OrdinaryPath(path)) throw new IOException("Settings recovery requires a regular settings file.");
        if (!List(path).Any(item => string.Equals(item.Path, backup, StringComparison.OrdinalIgnoreCase)))
            throw new IOException("This settings backup is unavailable or belongs to another career.");
        var previous = File.Exists(path) ? Prepare(path) : null;
        if (previous != null) File.Copy(path, previous);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".restore-tmp";
        try
        {
            File.Copy(backup, temporary);
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
