using System.IO.Compression;

namespace CarbonCompatLoader.Bootstrap.Gen;

public static class CFRelease
{
    public static void GenerateCFRelease(string path, string name, byte[] asm)
    {
        using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                archive.CreateEntry("carbon/");
                archive.CreateEntry("carbon/CCL/oxide/");
                archive.CreateEntry("carbon/CCL/harmony/");
                ZipArchiveEntry entry = archive.CreateEntry($"carbon/extensions/{name}", CompressionLevel.SmallestSize);
                using (Stream zs = entry.Open())
                {
                    zs.Write(asm);
                }
            }
        }
    }
}