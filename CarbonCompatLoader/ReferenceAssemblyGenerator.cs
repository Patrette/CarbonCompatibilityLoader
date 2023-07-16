using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using JetBrains.Refasmer;

namespace CarbonCompatLoader;

public static class ReferenceAssemblyGenerator
{
    public static unsafe byte[] ConvertToReferenceAssembly(byte[] asm)
    {
        fixed (byte* be = asm)
        {
            using PEReader reader = new(be, asm.Length);
            MetadataReader metadata = reader.GetMetadataReader();
            return MetadataImporter.MakeRefasm(metadata, reader, JBLogger);
        }
    }
    private static LoggerBase JBLogger = new LoggerBase(new DoNothingLogger());
    private class DoNothingLogger : ILogger
    {
        public void Log(LogLevel logLevel, string message) {}
        public bool IsEnabled(LogLevel logLevel) { return false; }
    }
}