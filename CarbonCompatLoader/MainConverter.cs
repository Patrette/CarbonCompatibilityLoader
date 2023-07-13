using System.Diagnostics;
using System.Reflection;
using API.Events;
using AsmResolver;
using AsmResolver.DotNet.Serialized;
using Carbon;
using Carbon.Core;
using CarbonCompatLoader.Converters;
using HarmonyLib;
using Microsoft.CodeAnalysis;

namespace CarbonCompatLoader;

public static class MainConverter
{
    public const string BuildConfiguration =
        #if DEBUG
            "Debug"
        #elif RELEASE
            "Release"
    #else
            this should not happen
    #endif
        ;

    public static string RootDir = Path.Combine(Defines.GetRootFolder(), "CCL");

    //public static ModuleDefinition SelfModule;

    public static AssemblyReference SDK = new AssemblyReference("Carbon.SDK", new Version(0, 0, 0, 0));

    public static AssemblyReference Common = new AssemblyReference("Carbon.Common", new Version(0, 0, 0, 0));

    public static Assembly CarbonMain;

    public static AssemblyReference Newtonsoft = new AssemblyReference("Newtonsoft.Json", new Version(0, 0, 0, 0));

    public static AssemblyReference protobuf = new AssemblyReference("protobuf-net", new Version(0, 0, 0, 0));

    public static AssemblyReference protobufCore = new AssemblyReference("protobuf-net.Core", new Version(0, 0, 0, 0));

    public static Harmony HarmonyInstance = new Harmony("patrette.CarbonCompatibilityLoader.core");

    public static Dictionary<string, BaseConverter> Converters;

    public static void LoadAssembly(string path, string fileName, string name, BaseConverter converter)
    {
        Stopwatch sw = Stopwatch.StartNew();
        ModuleDefinition md = ModuleDefinition.FromFile(path, new ModuleReaderParameters(EmptyErrorListener.Instance));
        if (AssemblyBlacklist.IsInvalid(md.Assembly))
        {
            Logger.Error($"{fileName} is invalid");
            return;
        }

        byte[] data = converter.Convert(md, out BaseConverter.GenInfo info);
        sw.Stop();
        Logger.Info($"Converted {converter.Path} assembly {name} in {sw.Elapsed.TotalMilliseconds:n0}ms");

    #if DEBUG
            File.WriteAllBytes(Path.Combine(RootDir, "debug_gen", fileName), data);
    #endif
        Assembly asm = Assembly.Load(data);
        if (converter.PluginReference) // harmony mods won't be used as plugin references
            if (!PluginReferenceHandler.RefCache.ContainsKey(name))
            {
                using (MemoryStream asmStream = new MemoryStream(data))
                    PluginReferenceHandler.RefCache.Add(name, MetadataReference.CreateFromStream(asmStream));
            }
            else
            {
                Logger.Error($"Cannot add {name} to the ref cache because it already exists??");
            }

        if (info.noEntryPoint) return;

        Type[] types;
        try
        {
            types = asm.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            types = e.Types;
        }

        types = types.Where(x => typeof(ICarbonCompatExt).IsAssignableFrom(x) && !x.IsAbstract).ToArray();
        if (types.Length == 0)
        {
            Logger.Error($"No entrypoint for {name}?!");
            return;
        }

        foreach (Type type in types)
        {
            ICarbonCompatExt ext = (ICarbonCompatExt)Activator.CreateInstance(type);
            ext.OnLoaded();
        }
    }

    public static void Initialize(string selfName)
    {
        if (Carbonara.CanRun()) Carbonara.Run();
        Converters = new Dictionary<string, BaseConverter>();
        // disabled for now
        //SelfModule = CCLCore.SelfASMRaw != null ? ModuleDefinition.FromBytes(CCLCore.SelfASMRaw) : ModuleDefinition.FromFile(Path.Combine(Defines.GetExtensionsFolder(), selfName));
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm == null) continue;
            AssemblyName asmName = asm.GetName();
            if (asmName.Name.StartsWith("Carbon_"))
            {
                CarbonMain = asm;
            #if DEBUG
                Logger.Info($"Found assembly: {asmName.Name}");
            #endif
                break;
            }
        #if DEBUG
            //Logger.Error($"Wrong assembly: {asmName.Name}");
        #endif
        }

        if (CarbonMain == null)
        {
            throw new NullReferenceException("Failed to find Carbon.dll??");
        }

        PluginReferenceHandler.ApplyPatch(CarbonMain);
        Directory.CreateDirectory(RootDir);
    #if DEBUG
        Directory.CreateDirectory(Path.Combine(RootDir, "debug_gen"));
    #endif
        foreach (Type type in Assembly.GetExecutingAssembly().GetTypes()
                     .Where(x => typeof(BaseConverter).IsAssignableFrom(x) && !x.IsAbstract))
        {
            BaseConverter cv = (BaseConverter)Activator.CreateInstance(type);
            if (Converters.TryGetValue(cv.Path, out BaseConverter dup))
            {
                Logger.Error($"Duplicate converter {type.FullName} > {dup.GetType().FullName}");
                continue;
            }

            cv.FullPath = Path.Combine(RootDir, cv.Path);
            Logger.Info($"Adding converter {cv.Path} : {cv.FullPath} > {type.FullName}");
            Directory.CreateDirectory(cv.FullPath);
            Converters.Add(cv.Path, cv);
        }
    }

    public static void LoadAll()
    {
        foreach (KeyValuePair<string, BaseConverter> kv in Converters)
        {
            string path = kv.Key;
            BaseConverter cv = kv.Value;
            Logger.Info($"Loading {path} mods");
            foreach (string asmPath in Directory.EnumerateFiles(cv.FullPath, "*.dll"))
            {
                string asmName = Path.GetFileNameWithoutExtension(asmPath);
                try
                {
                    Logger.Info($"Loading {asmName} using {cv.Path}");
                    LoadAssembly(asmPath, Path.GetFileName(asmPath), asmName, cv);
                }
                catch (Exception e)
                {
                    Logger.Error($"Failed to load {asmName} using {cv.Path}: {e}");
                }
            }
        }
    }
}