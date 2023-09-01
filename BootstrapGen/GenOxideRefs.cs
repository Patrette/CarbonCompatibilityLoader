using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using JetBrains.Refasmer;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Streams;
using Newtonsoft.Json;

namespace CarbonCompatLoader.Bootstrap.Gen;

public static class GenOxideRefs
{
    private static HttpClient client = new HttpClient();
    public static unsafe void Run(string path)
    {
        using (ZipArchive zip =
               new ZipArchive(client.GetStreamAsync("https://umod.org/games/rust/download?tag=public").Result))
        {
            Dictionary<string, string> manifest = new();
            foreach (ZipArchiveEntry entry in zip.Entries)
            {
                if (entry.Name.StartsWith("Oxide.") && entry.Name.EndsWith(".dll"))
                {
                    using (Stream dataStream = entry.Open())
                    {
                        ms.Position = 0;
                        ms.SetLength(0);
                        dataStream.CopyTo(ms);
                        fixed (byte* rb = ms.GetBuffer())
                        {
                            PEReader reader = new(rb, (int)ms.Length, false);
                            
                            MetadataReader metadata = reader.GetMetadataReader();
                            byte[] refData = MetadataImporter.MakeRefasm(metadata, reader, JBLogger);

                            ms.Position = 0;
                            ms.SetLength(0);

                            using (LZ4EncoderStream lz4 = LZ4Stream.Encode(ms, LZ4Level.L12_MAX, leaveOpen:true))
                            {
                                lz4.Write(refData, 0, refData.Length);
                            }


                            string asmName = metadata.GetAssemblyDefinition().GetAssemblyName().Name;

                            manifest[asmName] = $"{entry.Name}";

                            using (FileStream fs = new FileStream(Path.Combine(path, entry.Name), FileMode.Create))
                                ms.WriteTo(fs);
                        }
                    }
                }
            }
            File.WriteAllText(Path.Combine(path, "manifest.json"),JsonConvert.SerializeObject(manifest, Formatting.Indented));
        }
    }

    private static MemoryStream ms = new MemoryStream();
    private static readonly LoggerBase JBLogger = new LoggerBase(new jetlogger());
    private class jetlogger : JetBrains.Refasmer.ILogger
    {
        public void Log(LogLevel logLevel, string message) {}
        public bool IsEnabled(LogLevel logLevel) { return false; }
    }
}