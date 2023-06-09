using System.Reflection;
using System.Xml;
using Mono.Options;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

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

    public const string BuildConfiguration =
        #if DEBUG
            "Debug"
        #elif RELEASE
            "Release"
        #else
            this should not happen
        #endif
        ;
    public static List<XMLPackage> ParsePackagesXML(string data)
    {
        XmlDocument xml = new XmlDocument();
        xml.LoadXml(data);
        List<XMLPackage> op = new List<XMLPackage>();
        foreach (XmlNode node in xml.GetElementsByTagName("package"))
        {
            if (node.Attributes == null) continue;
            string id = null;
            string version = null;
            foreach (XmlAttribute attr in node.Attributes)
            {
                switch (attr.Name)
                {
                    case "id":
                        id = attr.Value;
                        break;
                    case "version":
                        version = attr.Value;
                        break;
                    default:
                        continue;
                }
                if (id != null && version != null)
                {
                    op.Add(new XMLPackage(id, version));
                    break;
                }
            }
        }

        return op;
    }

    public record XMLPackage(string id, string version);
    public static void Main(string[] args)
    {
        Bootstrap.logger = new ConsoleLogger();
        string inputPath = null;
        //string outputPath = null;
        List<string> outPaths = new List<string>();
        string NuGetCache = null;
        string ExtPath = null;
        string infoPath = null;
        string packagesPath = null;
        string rootBuildPath = null;
        OptionSet optionSet = new OptionSet()
        {
            { "in|input|i=", x => {inputPath = string.Format(x, BuildConfiguration);} },
            { "o|out|op|output=", x => { outPaths.Add(x);} },
            { "nuget|ng=", x => { NuGetCache = x;} },
            { "ext|ep=", x => { ExtPath = string.Format(x, BuildConfiguration);} },
            { "info=", x => { infoPath = x;} },
            { "package|packages|pk=", x => { packagesPath = x;} },
            { "bp|build|buildpath=", x => { rootBuildPath = x;} }
        };
        optionSet.Parse(args);
        if (inputPath == null || rootBuildPath == null ||  ExtPath == null || infoPath == null)
        {
            Bootstrap.logger.Error("No args??");
            return;
        }

        List<XMLPackage> xml = null;

        if (packagesPath != null)
        {
            xml = ParsePackagesXML(File.ReadAllText(packagesPath));
        }

        string buildPath = Path.Combine(rootBuildPath, BuildConfiguration);
        Directory.CreateDirectory(buildPath);

        AssemblyDownloader.WriteCache = true;
        AssemblyDownloader.NuGetCache = NuGetCache;
        
        Bootstrap.downloadInfo = JsonConvert.DeserializeObject<DownloadManifest>(File.ReadAllText(infoPath));
        
        AssemblyName extMeta = AssemblyName.GetAssemblyName(ExtPath);
        AssemblyName bootstrapMeta = AssemblyName.GetAssemblyName(inputPath);
        
        Bootstrap.downloadInfo.extensionName = extMeta.Name+".dll";
        Bootstrap.downloadInfo.bootstrapName = bootstrapMeta.Name+".dll";
        
        Bootstrap.downloadInfo.extensionVersion = extMeta.Version.ToString();
        Bootstrap.downloadInfo.bootstrapVersion = bootstrapMeta.Version.ToString();
        if (xml != null)
            foreach (DownloadManifest.DLDependency dep in Bootstrap.downloadInfo.dependencies.ToList())
            {
                if (dep.version == null)
                {
                    XMLPackage entry = xml.FirstOrDefault(x => x.id == dep.id);
                    if (entry == null)
                    {
                        Bootstrap.downloadInfo.dependencies.Remove(dep);
                        continue;
                    }

                    dep.version = entry.version;
                }
            }

        byte[] extData = File.ReadAllBytes(ExtPath);

        byte[] bootstrapData = File.ReadAllBytes(inputPath);

        File.WriteAllBytes(Path.Combine(buildPath, Bootstrap.downloadInfo.extensionName), extData);
        
        File.WriteAllText(Path.Combine(buildPath, "info.json"), JsonConvert.SerializeObject(Bootstrap.downloadInfo, Formatting.Indented));
        
        Bootstrap.Run(inputPath, Path.Combine(buildPath, Bootstrap.downloadInfo.bootstrapName), out byte[] asmGenData, bootstrapData: bootstrapData, extData: extData);

        foreach (string op in outPaths)
        {
            File.WriteAllBytes(op, asmGenData);
        }
    }
}