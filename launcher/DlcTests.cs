using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Hope.Launcher;

internal static class DlcTests
{
    public static void Run(string root, Action<bool, string> check, string? linkFixture)
    {
        Directory.CreateDirectory(root);
        var bundle = Path.Combine(root, "HOPE");
        Directory.CreateDirectory(bundle);
        File.WriteAllText(Path.Combine(bundle, "portable.txt"), "");
        var fixture = new byte[DlcLibrary.HeaderBytes + 4096];
        Encoding.ASCII.GetBytes("LIVE").CopyTo(fixture, 0);
        void Field(int offset, uint value) => BinaryPrimitives.WriteUInt32BigEndian(fixture.AsSpan(offset, 4), value);
        Field(0x340, DlcLibrary.HeaderBytes); Field(0x344, 2); Field(0x348, 2); Field(0x360, 0x454108E6);
        Encoding.BigEndianUnicode.GetBytes("Synthetic DLC header fixture").CopyTo(fixture, 0x411);
        var source = Path.Combine(root, "extensionless");
        File.WriteAllBytes(source, fixture);
        var original = SHA256.HashData(fixture);
        check(DlcLibrary.Read(source).Name == "Synthetic DLC header fixture", "DLC header display name decoded as UTF-16 big endian");
        foreach (var magic in new[] { "CON ", "LIVE", "PIRS" })
        {
            Encoding.ASCII.GetBytes(magic).CopyTo(fixture, 0);
            File.WriteAllBytes(source, fixture);
            check(DlcLibrary.Read(source).Bytes == fixture.Length, $"{magic.Trim()} DLC header supported");
        }
        Encoding.ASCII.GetBytes("LIVE").CopyTo(fixture, 0);
        File.WriteAllBytes(source, fixture);
        var result = DlcLibrary.Import(source, bundle, () => false);
        var packages = DlcLibrary.List(bundle, out var invalid);
        check(result.Contains("next launch") && packages.Count == 1 && invalid == 0, "Extensionless DLC copied and queued, not claimed installed");
        check(Path.GetFileName(packages[0].Path).Length == 40, "DLC identity fits the runtime's 42-byte filename field");
        check(SHA256.HashData(File.ReadAllBytes(source)).SequenceEqual(original), "DLC source remains unchanged");
        check(DlcLibrary.Import(source, bundle, () => false).Contains("already imported"), "Duplicate import preserves existing package");
        var renamed = Path.Combine(root, "renamed.dlc");
        File.Copy(source, renamed);
        check(DlcLibrary.Import(renamed, bundle, () => false).Contains("already imported"), "Renamed identical DLC deduplicates");
        bool Reject(Action action) { try { action(); return false; } catch (IOException) { return true; } }
        check(Reject(() => DlcLibrary.Import(source, bundle, () => true)), "DLC import blocked while game is running");
        var calls = 0;
        check(Reject(() => DlcLibrary.Import(source, bundle, () => ++calls > 1)), "Game-open race checked again before committing DLC");
        check(!Directory.EnumerateFiles(Path.Combine(bundle, ".hope-dlc-import")).Any(), "Temporary DLC files cleaned after duplicate or blocked commit");
        File.WriteAllText(packages[0].Path, "damaged sentinel");
        check(Reject(() => DlcLibrary.Import(source, bundle, () => false)) && File.ReadAllText(packages[0].Path) == "damaged sentinel", "Damaged destination cannot be overwritten silently");
        foreach (var bad in new[] { (0x344, 1u), (0x344, 0xB0000u), (0x360, 0u), (0x360, 0x454108F0u), (0x348, 3u), (0x3A9, 1u), (0x340, 20u), (0x340, (uint)fixture.Length + 1) })
        {
            var bytes = (byte[])fixture.Clone();
            BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(bad.Item1, 4), bad.Item2);
            var path = Path.Combine(root, "invalid"); File.WriteAllBytes(path, bytes);
            check(Reject(() => DlcLibrary.Import(path, bundle, () => false)), $"Unsupported DLC field {bad.Item1:X}/{bad.Item2:X} rejected");
        }
        var truncated = Path.Combine(root, "truncated"); File.WriteAllBytes(truncated, fixture[..100]);
        check(Reject(() => DlcLibrary.Read(truncated)), "Truncated DLC header rejected");
        var zip = Path.Combine(root, "archive.zip"); File.WriteAllBytes(zip, new byte[fixture.Length]);
        check(Reject(() => DlcLibrary.Read(zip)), "Non-package data rejected");
        using (var locked = new FileStream(source, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            check(Reject(() => DlcLibrary.Import(source, bundle, () => false)), "Locked source rejected without committing partial DLC");
        packages = DlcLibrary.List(bundle, out invalid);
        check(packages.Count == 0 && invalid == 1, "Unreadable imported package reported in list");
        var settings = Path.Combine(bundle, "settings.toml"); File.WriteAllText(settings, "vsync = true\n");
        File.WriteAllText(Path.Combine(bundle, "skate3.exe"), "fixture only");
        File.WriteAllText(Path.Combine(bundle, "rexruntime.dll"), "fixture only");
        Directory.CreateDirectory(Path.Combine(bundle, "game"));
        File.WriteAllText(Path.Combine(bundle, "game", "default.xex"), "fixture only");
        var state = new LauncherState(bundle);
        var info = state.BuildStartInfo();
        check(info.ArgumentList.Contains("--skate3_dlc_root=" + DlcLibrary.Root(bundle)) && info.Environment["REX_SKATE3_AUTO_INSTALL_DLC"] == "true", "Main career launch enables shared DLC without launching game");
        var career = Careers.Create(state); state.SelectCareer(career);
        info = state.BuildStartInfo();
        check(info.Environment["REX_SKATE3_DLC_ROOT"] == DlcLibrary.Root(bundle) && info.WorkingDirectory != bundle, "Isolated career launch uses same DLC library");
        check(File.ReadAllText(settings) == "vsync = true\n", "DLC launch setup leaves stored graphics settings untouched");
        using (var window = new MainWindow(bundle))
        {
            window.Navigate("Setup");
            check(window.DlcStatus.Text.Contains("could not") && window.ImportDlcButton.IsEnabled, "Game setup exposes DLC import and unreadable-package status");
        }
        if (linkFixture != null)
            check(Reject(() => DlcLibrary.Import(source, Path.Combine(linkFixture, "packages", "LINKED_SAVE"), () => false)), "DLC importer refuses a linked destination root");
    }
}
