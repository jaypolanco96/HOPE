using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Hope.Launcher;

public sealed record DlcPackage(string Path, string Name, long Bytes)
{
    public string Label => $"{Name}  ·  {Bytes / 1048576d:0.0} MB · Imported";
}

public static class DlcLibrary
{
    public const int HeaderBytes = 0x971A;
    public static string Root(string bundleRoot) => Path.Combine(bundleRoot, "dlc");

    // These offsets match ReXGlue's packed XContentHeader/XContentMetadata.
    // Header validation identifies the package; the runtime validates/extracts
    // the filesystem at launch. This does not certify payload compatibility.
    public static DlcPackage Read(string path)
    {
        if (!SaveStorage.OrdinaryPath(path)) throw new IOException("Linked DLC files or folders cannot be imported.");
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Read(stream, path);
    }

    private static DlcPackage Read(Stream stream, string path)
    {
        if (stream.Length < HeaderBytes) throw new IOException("Not a complete Xbox 360 package header.");
        var header = new byte[HeaderBytes];
        stream.ReadExactly(header);
        uint Field(int offset) => BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(offset, 4));
        var magic = Encoding.ASCII.GetString(header, 0, 4);
        if (magic is not ("CON " or "LIVE" or "PIRS")) throw new IOException("Choose an Xbox 360 DLC package, not an ISO, ZIP or extracted folder.");
        if (Field(0x344) != 2) throw new IOException("This is not a downloadable-content package (saves and title updates are not DLC).");
        if (Field(0x360) != 0x454108E6) throw new IOException("This package is not identified as Skate 3 DLC.");
        if (Field(0x348) is not (1 or 2) || Field(0x340) < HeaderBytes || Field(0x340) > stream.Length)
            throw new IOException("The package header is incomplete or unsupported.");
        if (Field(0x3A9) != 0) throw new IOException("This package requires companion data files. Import a single-file STFS DLC package instead.");
        var title = Encoding.BigEndianUnicode.GetString(header, 0x411, 256).Split('\0')[0];
        title = new string(title.Where(c => !char.IsControl(c)).ToArray()).Trim();
        return new(path, title.Length == 0 ? System.IO.Path.GetFileName(path) : title, stream.Length);
    }

    public static List<DlcPackage> List(string bundleRoot, out int unreadable)
    {
        unreadable = 0;
        var root = Root(bundleRoot);
        if (!SaveStorage.OrdinaryPath(root)) throw new IOException("The DLC folder is linked. Choose an ordinary HOPE installation folder.");
        var packages = new List<DlcPackage>();
        if (!Directory.Exists(root)) return packages;
        foreach (var file in Directory.EnumerateFiles(root).Order())
        {
            try { packages.Add(Read(file)); }
            catch (IOException) { unreadable++; }
            catch (UnauthorizedAccessException) { unreadable++; }
        }
        return packages;
    }

    public static string Import(string source, string bundleRoot, Func<bool> gameRunning)
    {
        void RequireClosed() { if (gameRunning()) throw new IOException("Close Skate 3 before importing DLC."); }
        RequireClosed();
        source = Path.GetFullPath(source);
        var root = Root(bundleRoot);
        var staging = Path.Combine(bundleRoot, ".hope-dlc-import");
        if (!SaveStorage.OrdinaryPath(source) || !SaveStorage.OrdinaryPath(root) || !SaveStorage.OrdinaryPath(staging))
            throw new IOException("Linked DLC files or folders cannot be imported.");
        using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read);
        var package = Read(input, source);
        input.Position = 0;
        Directory.CreateDirectory(staging);
        var temporary = Path.Combine(staging, Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            string fingerprint;
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                var buffer = new byte[1024 * 1024];
                int count;
                while ((count = input.Read(buffer)) > 0) { output.Write(buffer, 0, count); hash.AppendData(buffer, 0, count); }
                output.Flush(true);
                fingerprint = Convert.ToHexString(hash.GetHashAndReset());
            }
            RequireClosed();
            Directory.CreateDirectory(root);
            // Use a bounded filename accepted by XCONTENT_AGGREGATE_DATA.
            var destination = Path.Combine(root, fingerprint[..40]);
            if (!SaveStorage.OrdinaryPath(destination)) throw new IOException("The DLC destination is linked.");
            if (File.Exists(destination))
            {
                using var existing = File.OpenRead(destination);
                if (Convert.ToHexString(SHA256.HashData(existing)) != fingerprint)
                    throw new IOException("A different or damaged package occupies this DLC filename. Nothing was replaced.");
                return $"{package.Name}: already imported.";
            }
            File.Move(temporary, destination);
            return $"{package.Name}: imported. The game will attempt installation next launch.";
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
