using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;
using Mono.Cecil;
using Newtonsoft.Json;

namespace CarbonCompatLoader.Bootstrap;

public interface ILogger
{
    public void Info(object obj);

    public void Warn(object obj);

    public void Error(object obj);
}

public static class Bootstrap
{
    public static Assembly selfAsm = Assembly.GetExecutingAssembly();
    public readonly static Version Version = null;
    public readonly static string VersionString = null;
    static Bootstrap()
    {
        Version = selfAsm.GetName().Version;
        VersionString = Version.ToString();
    }

    public const string extensionResourceName = "core.dll";
    public const string infoResourceName = "info.json";
    public const string dependencyFormatString = "dep_{0}.dll";
    public static ILogger logger;
    public static AssemblyManifest asmInfo;
    public static DownloadManifest downloadInfo;
    public static bool TryGetResourceBytes(string name, Assembly asm, out byte[] data)
    {
        if (!asm.GetManifestResourceNames().Contains(name))
        {
            data = null;
            return false;
        }

        using (MemoryStream ms = new MemoryStream())
        {
            using (Stream str = asm.GetManifestResourceStream(name))
            {
                if (str == null)
                {
                    data = null;
                    return false;
                }
                str.CopyTo(ms);
                data = ms.ToArray();
            }
        }

        return true;
    }
    public static bool TryGetResourceString(string name, Assembly asm, out string data)
    {
        if (!asm.GetManifestResourceNames().Contains(name))
        {
            data = null;
            return false;
        }

        using (Stream str = asm.GetManifestResourceStream(name))
        {
            if (str == null)
            {
                data = null;
                return false;
            }

            using (StreamReader sr = new StreamReader(str, Encoding.ASCII))
            {
                data = sr.ReadToEnd();
            }
        }
        
        return true;
    }
    public static bool TryGetResourceStream(string name, Assembly asm, out Stream data)
    {
        if (!asm.GetManifestResourceNames().Contains("info.json"))
        {
            data = null;
            return false;
        }

        Stream str = asm.GetManifestResourceStream(name);

        if (str == null)
        {
            data = null;
            return false;
        }

        data = str;

        return true;
    }
    public static bool TryGetLZ4ResourceStream(string name, Assembly asm, out Stream data)
    {
        if (!TryGetResourceStream(name, asm, out data)) return false;
        data = LZ4Stream.Decode(data, interactive:true, leaveOpen:true);
        return true;
    }
    public static void AddResource(this ModuleDefinition module, string name, byte[] bytes)
    {
        module.Resources.Add(new EmbeddedResource(name, ManifestResourceAttributes.Private, bytes));
    }
    public static void AddLZ4Resource(this ModuleDefinition module, string name, byte[] data)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            using (LZ4EncoderStream lz4 = LZ4Stream.Encode(ms, LZ4Level.L12_MAX))
            {
                lz4.Write(data, 0, data.Length);
            }
            module.Resources.Add(new EmbeddedResource(name, ManifestResourceAttributes.Private, ms.ToArray()));
        }
    }
    public static void Run(string asmReadPath, string asmWritePath, out byte[] asmOut, string branch = "prod", byte[] extData = null, byte[] bootstrapData = null, bool load = false)
    {
        asmOut = null;
        logger.Info($"Input: {asmReadPath}");
        logger.Info($"Output: {asmWritePath}");
        asmInfo = TryGetResourceString(infoResourceName, selfAsm, out string infoStr) ? JsonConvert.DeserializeObject<AssemblyManifest>(infoStr) : new AssemblyManifest();
        bool needsRepack = false;
        bool updateFail = true;
        byte[] newExtData = null;
        if (downloadInfo == null)
        {
            logger.Info("Downloading latest version info");
            if (!AssemblyDownloader.DownloadStringFromGithubRelease(branch, "build.info", out string dlStr))
            {
                logger.Error("Failed to download build info");
                goto end;
            }
            downloadInfo = JsonConvert.DeserializeObject<DownloadManifest>(dlStr);
        }
        
        if (downloadInfo.extensionVersion != asmInfo.extensionVersion && extData == null)
        {
            logger.Info($"Downloading extension version {downloadInfo.extensionVersion} > {asmInfo.extensionVersion}");
            if (!AssemblyDownloader.DownloadBytesFromGithubRelease(branch, downloadInfo.extensionName, out newExtData))
            {
                logger.Error("Failed to download extension");
                goto end;
            }

            needsRepack = true;
        }
        else if (extData == null)
        {
            if (!asmInfo.ResolveCore(selfAsm, ref extData))
            {
                asmInfo.valid = false;
                goto end;
            }
        }
        
        if (downloadInfo.bootstrapVersion != VersionString && bootstrapData == null)
        {
            logger.Info($"Downloading bootstrap version {downloadInfo.bootstrapVersion} > {VersionString}");
            if (!AssemblyDownloader.DownloadBytesFromGithubRelease(branch, downloadInfo.bootstrapName, out bootstrapData))
            {
                logger.Error("Failed to download bootstrap");
                goto end;
            }

            needsRepack = true;
        }

        List<AssemblyManifest.ASMDependency> newDeps = new List<AssemblyManifest.ASMDependency>();
        foreach (DownloadManifest.DLDependency dep in downloadInfo.dependencies)
        {
            AssemblyManifest.ASMDependency asmDep = asmInfo.dependencies.FirstOrDefault(x => x.id == dep.id && x.version == dep.version);
            if (asmDep != null)
            {
                if (!asmDep.LoadDataFromASM(selfAsm))
                {
                    logger.Error($"Failed to find dependency {asmDep.id} version {asmDep.version}, downloading");
                    goto dl;
                }
                newDeps.Add(asmDep);
                continue;
            }
            dl:
            AssemblyManifest.ASMDependency nd = dep.ToASM();
            if (!AssemblyDownloader.DownloadNuGetPackage(nd.id, nd.version, out nd.data))
            {
                logger.Error($"Failed to download {nd.id} version {nd.version} using NuGet");
                goto end;
            }
            newDeps.Add(nd);
            
            needsRepack = true;
        }
        
        asmInfo.dependencies.Clear();
        asmInfo.dependencies = newDeps;

        if (!load) asmInfo.extensionVersion = downloadInfo.extensionVersion;

        if (newExtData != null)
        {
            asmInfo.extensionVersion = downloadInfo.extensionVersion;
            extData = newExtData;
        }

        asmInfo.valid = true;

        updateFail = false;

        if (needsRepack)
        {
            logger.Info("Repacking assembly");
            
            AssemblyDefinition asmDef;
            Stream asmStream = null;
            try
            {
                if (bootstrapData == null)
                {
                    asmStream = new FileStream(asmReadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                else
                {
                    asmStream = new MemoryStream(bootstrapData);
                }

                asmDef = AssemblyDefinition.ReadAssembly(asmStream);

                asmDef.MainModule.Resources.Clear();
                asmDef.MainModule.AddLZ4Resource(extensionResourceName, extData);
                int index = 0;
                foreach (AssemblyManifest.ASMDependency dep in asmInfo.dependencies)
                {
                    foreach (byte[] data in dep.data)
                    {
                        asmDef.MainModule.AddLZ4Resource(string.Format(dependencyFormatString, index), data);
                        dep.installed.Add(index);

                        index++;
                    }
                }

                asmDef.MainModule.AddResource(infoResourceName, Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(asmInfo, Formatting.Indented)));
                using (MemoryStream ms = new MemoryStream())
                {
                    asmDef.Write(ms);
                    asmOut = ms.ToArray();
                    File.WriteAllBytes(asmWritePath, asmOut);
                    
                }
            }
            catch (Exception e)
            {
                logger.Error($"Failed to repack assembly: {e}");
            }
            finally
            {
                asmStream?.Dispose();
            }
        }
        end:
        if (!asmInfo.valid || !asmInfo.EnsureResolved(selfAsm, ref extData))
        {
            logger.Error("Failed to start :/");
            return;
        }

        if (updateFail)
        {
            logger.Warn($"Failed to update, falling back to version {asmInfo.extensionVersion}");
        }

        if (!load)
        {
            logger.Info("Generation complete");
            return;
        }
        logger.Info("Launching");
        List<byte[]> raw_deps = new List<byte[]>();
        foreach (AssemblyManifest.ASMDependency dep in asmInfo.dependencies)
        {
            raw_deps.AddRange(dep.data);
        }
        CarbonContainer.Load(extData, raw_deps, asmInfo.extensionVersion);
    }
}