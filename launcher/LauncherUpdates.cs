using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Hope.Launcher;

internal sealed record HopeRelease(string Tag, Version Version, string Url, string Digest, long Size);

internal static class LauncherUpdates
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(10) };
    static LauncherUpdates() { Client.DefaultRequestHeaders.UserAgent.ParseAdd("HOPE-Launcher/0.6.6"); }
    public static Version Installed => Assembly.GetExecutingAssembly().GetName().Version!;

    internal static HopeRelease Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var release = doc.RootElement;
        if (release.GetProperty("draft").GetBoolean() || release.GetProperty("prerelease").GetBoolean())
            throw new IOException("No public release is available.");
        var tag = release.GetProperty("tag_name").GetString()!;
        var match = Regex.Match(tag, @"(?:^|-)v?(\d+\.\d+\.\d+)(?:$|-)");
        if (!match.Success) throw new IOException("The release version could not be read.");
        var assets = release.GetProperty("assets").EnumerateArray().Where(a =>
            Regex.IsMatch(a.GetProperty("name").GetString()!, @"^HOPE-[\d.]+-windows-x64(?:-preview)?\.zip$")).ToArray();
        if (assets.Length != 1) throw new IOException("This release has no unique Windows x64 package.");
        var asset = assets[0];
        var url = asset.GetProperty("browser_download_url").GetString()!;
        var uri = new Uri(url);
        if (uri.Scheme != "https" || uri.Host != "github.com" ||
            !uri.AbsolutePath.StartsWith("/jaypolanco96/HOPE/releases/download/", StringComparison.Ordinal))
            throw new IOException("Unexpected update download address.");
        var digest = asset.GetProperty("digest").GetString() ?? "";
        if (!Regex.IsMatch(digest, @"^sha256:[a-fA-F0-9]{64}$"))
            throw new IOException("This release has no verifiable download checksum.");
        var size = asset.GetProperty("size").GetInt64();
        if (size <= 0 || size > 512L * 1024 * 1024) throw new IOException("Unexpected update size.");
        return new(tag, Version.Parse(match.Groups[1].Value), url, digest[7..], size);
    }

    public static async Task<HopeRelease> CheckAsync() => Parse(await Client.GetStringAsync(
        "https://api.github.com/repos/jaypolanco96/HOPE/releases/latest"));

    internal static bool IsProgram(string name) => Regex.IsMatch(name,
        @"^(?:[A-Za-z0-9_.-]+\.(?:dll|exe|pdb)|HOPE\.(?:deps|runtimeconfig)\.json|[a-z]{2}(?:-[A-Za-z]{2,4})?/[A-Za-z0-9_.-]+\.resources\.dll)$");

    internal static void ExtractPrograms(string archive, string stage)
    {
        var payload = Path.Combine(stage, "payload");
        Directory.CreateDirectory(payload);
        using var zip = ZipFile.OpenRead(archive);
        if (zip.Entries.Count > 3000 || zip.Entries.Sum(e => e.Length) > 1500L * 1024 * 1024)
            throw new IOException("The update archive is too large.");
        var roots = zip.Entries.Where(e => e.FullName.EndsWith("/HOPE.exe", StringComparison.Ordinal))
            .Select(e => e.FullName[..^8]).Distinct().ToArray();
        if (roots.Length != 1 || roots[0].Count(c => c == '/') != 1)
            throw new IOException("Unexpected update folder layout.");
        var files = new List<object>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in zip.Entries) {
            if (!entry.FullName.StartsWith(roots[0], StringComparison.Ordinal)) continue;
            var name = entry.FullName[roots[0].Length..];
            if (!IsProgram(name)) continue;
            if (!seen.Add(name)) throw new IOException("Duplicate update program file.");
            var destination = Path.Combine(payload, name.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            entry.ExtractToFile(destination);
            using var file = File.OpenRead(destination);
            files.Add(new { name, sha256 = Convert.ToHexString(SHA256.HashData(file)) });
        }
        foreach (var required in new[] { "HOPE.exe", "HOPE.dll", "HOPE.deps.json", "HOPE.runtimeconfig.json", "skate3.exe", "rexruntimerd.dll" })
            if (!seen.Contains(required)) throw new IOException("The update is missing " + required);
        File.WriteAllText(Path.Combine(stage, "payload.json"), JsonSerializer.Serialize(new { files }));
    }

    public static async Task<string> PrepareAsync(HopeRelease release, IProgress<string> progress)
    {
        var stage = Path.Combine(Path.GetTempPath(), "hope-update-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stage);
        var archive = Path.Combine(stage, "release.zip");
        using (var response = await Client.GetAsync(release.Url, HttpCompletionOption.ResponseHeadersRead)) {
            response.EnsureSuccessStatusCode();
            await using var input = await response.Content.ReadAsStreamAsync();
            await using var output = File.Create(archive);
            var buffer = new byte[128 * 1024];
            long total = 0;
            int read;
            while ((read = await input.ReadAsync(buffer)) != 0) {
                total += read;
                if (total > release.Size) throw new IOException("The download exceeds its expected size.");
                await output.WriteAsync(buffer.AsMemory(0, read));
                progress.Report($"Downloading HOPE… {total * 100 / release.Size}%");
            }
            if (total != release.Size) throw new IOException("The update download is incomplete.");
        }
        progress.Report("Checking download and preparing installation…");
        await Task.Run(() => {
            using var input = File.OpenRead(archive);
            if (!Convert.ToHexString(SHA256.HashData(input)).Equals(release.Digest, StringComparison.OrdinalIgnoreCase))
                throw new IOException("The update checksum does not match. Nothing was installed.");
            ExtractPrograms(archive, stage);
        });
        using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Hope.UpdateInstaller.ps1")!;
        using var reader = new StreamReader(resource);
        File.WriteAllText(Path.Combine(stage, "Install.ps1"), reader.ReadToEnd());
        return stage;
    }

    public static void StartInstaller(string stage, string root)
    {
        var job = new { root = Path.GetFullPath(root), pid = Environment.ProcessId };
        File.WriteAllText(Path.Combine(stage, "job.json"), JsonSerializer.Serialize(job));
        File.WriteAllText(Path.Combine(stage, "Run.ps1"), """
            $ErrorActionPreference = 'Stop'
            $job = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'job.json') -Raw | ConvertFrom-Json
            $result = Join-Path $job.root 'hope-update-result.json'
            try {
                $parent = Get-Process -Id $job.pid -ErrorAction SilentlyContinue
                if ($parent -and !$parent.WaitForExit(90000)) { throw 'HOPE did not close; no files were changed.' }
                & (Join-Path $PSScriptRoot 'Install.ps1') -Target $job.root -ReleasePackage
                @{message='HOPE updated successfully. Your saves and settings were preserved.'} | ConvertTo-Json | Set-Content -LiteralPath $result
            } catch {
                @{message=('Update failed: ' + $_.Exception.Message)} | ConvertTo-Json | Set-Content -LiteralPath $result
            }
            Start-Process -FilePath (Join-Path $job.root 'HOPE.exe') -WorkingDirectory $job.root -WindowStyle Hidden
            """);
        var start = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", Path.Combine(stage, "Run.ps1") })
            start.ArgumentList.Add(argument);
        _ = Process.Start(start) ?? throw new IOException("Could not start the update installer.");
    }
}
