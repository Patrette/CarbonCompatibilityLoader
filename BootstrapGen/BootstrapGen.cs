using Mono.Options;

namespace CarbonCompatLoader.Bootstrap.Gen;

public static class BootstrapGen
{
    public class ConsoleLogger : ILogger
    {
        public void Info(object obj)
        {
            Console.WriteLine($"[Info] {obj}");
        }
        public void Warn(object obj)
        {
            Console.WriteLine($"[Warn] {obj}");
        }
        public void Error(object obj)
        {
            Console.WriteLine($"[Error] {obj}");
        }
    }
    public static void Main(string[] args)
    {
        Bootstrap.logger = new ConsoleLogger();
        string inputPath = null;
        string outputPath = null;
        string NuGetCache = null;
        string ExtPath = null;
        OptionSet optionSet = new OptionSet()
        {
            { "in|input|i=", x => { inputPath = x;} },
            { "o|out|op|output=", x => { outputPath = x;} },
            { "nuget|ng=", x => { NuGetCache = x;} },
            { "ext|ep=", x => { ExtPath = x;} },
        };
        optionSet.Parse(args);
        if (inputPath == null || outputPath == null || ExtPath == null)
        {
            Bootstrap.logger.Error("No args??");
            return;
        }
        
        AssemblyDownloader.WriteCache = true;
        AssemblyDownloader.NuGetCache = NuGetCache;
        Bootstrap.downloadInfo = new DownloadManifest()
        {
            extensionName = "core.dll",
            bootstrapName = "bootstrap.dll",
            bootstrapVersion = "1.0.0.0",
            extensionVersion = "1.0.0.0",
            dependencies = new List<DownloadManifest.DLDependency>()
            {
                new DownloadManifest.DLDependency()
                {
                    type = AssemblySource.NuGet,
                    id = "AsmResolver",
                    version = "5.3.0"
                },
                new DownloadManifest.DLDependency()
                {
                    type = AssemblySource.NuGet,
                    id = "AsmResolver.DotNet",
                    version = "5.3.0"
                },
                new DownloadManifest.DLDependency()
                {
                    type = AssemblySource.NuGet,
                    id = "AsmResolver.PE",
                    version = "5.3.0"
                },
                new DownloadManifest.DLDependency()
                {
                    type = AssemblySource.NuGet,
                    id = "AsmResolver.PE.File",
                    version = "5.3.0"
                },
            }
        };
        Bootstrap.Run(inputPath, outputPath, bootstrapData: File.ReadAllBytes(inputPath), extData: File.ReadAllBytes(ExtPath));
    }
}