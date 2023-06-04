using System.Diagnostics;
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
                if (attr.Name == "id")
                {
                    id = attr.Value;
                    continue;
                }
                if (attr.Name == "version")
                {
                    version = attr.Value;
                    continue;
                }
            }
            if (id != null && version != null
                           //&& !(id.ToLower().StartsWith("system") || id.ToLower().StartsWith("microsoft"))
                ) op.Add(new XMLPackage(id, version));
        }

        return op;
    }

    public class XMLPackage
    {
        public string id;
        public string version;

        public XMLPackage(string id, string version)
        {
            this.id = id;
            this.version = version;
        }
    }
    public static void Main(string[] args)
    {
        Bootstrap.logger = new ConsoleLogger();
        string inputPath = null;
        string outputPath = null;
        string NuGetCache = null;
        string ExtPath = null;
        string infoPath = null;
        string packagesPath = null;
        string rootBuildPath = null;
        OptionSet optionSet = new OptionSet()
        {
            { "in|input|i=", x => { inputPath = x;} },
            //{ "o|out|op|output=", x => { outputPath = x;} },
            { "nuget|ng=", x => { NuGetCache = x;} },
            { "ext|ep=", x => { ExtPath = x;} },
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

        File.WriteAllBytes(Path.Combine(buildPath, Bootstrap.downloadInfo.extensionName), extData);
        
        File.WriteAllText(Path.Combine(buildPath, "info.json"), JsonConvert.SerializeObject(Bootstrap.downloadInfo, Formatting.Indented));
        
        Bootstrap.Run(inputPath, Path.Combine(buildPath, Bootstrap.downloadInfo.bootstrapName), bootstrapData: File.ReadAllBytes(inputPath), extData: extData);
    }
}