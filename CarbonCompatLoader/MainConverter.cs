using System.Diagnostics;
using System.Reflection;
using AsmResolver;
using AsmResolver.DotNet.Serialized;
using Carbon.Core;
using CarbonCompatLoader.Converters;
using HarmonyLib;
using ILVerify;
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
        byte[] originalASM = File.ReadAllBytes(path);
        VerificationResult[] originalResults = ILVerifier.VerifyAssembly(originalASM);
        ModuleDefinition md = ModuleDefinition.FromBytes(originalASM, new ModuleReaderParameters(EmptyErrorListener.Instance));
        if (AssemblyBlacklist.IsInvalid(md.Assembly))
        {
            Logger.Error($"{fileName} is invalid");
            return;
        }

        byte[] data = converter.Convert(md, out BaseConverter.GenInfo info);
        VerificationResult[] convertedResults = ILVerifier.VerifyAssembly(data, true);
        ILVerifier.ProcessResults(originalResults, convertedResults, md, info.mappings);
        if (CCLEntrypoint.InitialConfig?.Development?.ReferenceAssemblies != null &&
            CCLEntrypoint.InitialConfig.Development.DevMode &&
            CCLEntrypoint.InitialConfig.Development.ReferenceAssemblies.Contains(md.Assembly?.Name))
        {
            Logger.Info($"Generating reference assembly for {name}");
            try
            {
                File.WriteAllBytes(Path.Combine(RootDir, "ReferenceAssemblies", fileName), ReferenceAssemblyGenerator.ConvertToReferenceAssembly(data));
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to generate reference assembly for {name}: \n{e}");
            }
        }
        sw.Stop();
        Logger.Info($"Converted {converter.Path} assembly {name} in {sw.Elapsed.TotalMilliseconds:n0}ms");

    #if DEBUG
        File.WriteAllBytes(Path.Combine(RootDir, "debug_gen", fileName), data);
    #endif
        Assembly asm = Assembly.Load(data);
        if (converter.PluginReference) // harmony mods won't be used as plugin references
            if (!PluginReferenceHandler.RefCache.ContainsKey(name))
            {
                using MemoryStream asmStream = new MemoryStream(data);
                PluginReferenceHandler.RefCache.Add(name, MetadataReference.CreateFromStream(asmStream));
            }
            else
            {
                Logger.Error($"Cannot add {name} to the ref cache because it already exists??");
            }

        if (info.noEntryPoint) return;

        Type[] types = GetTypesWithoutError(asm);

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

    public static Type[] GetTypesWithoutError(Assembly asm)
    {
        try
        {
            return asm.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types;
        }
    }

    public static byte[] ThisASM;

    public static void Initialize(string selfName)
    {
        if (Carbonara.CanRun()) Carbonara.Run();
        Converters = new Dictionary<string, BaseConverter>();
        // disabled for now
        ThisASM = CCLEntrypoint.SelfASMRaw ?? File.ReadAllBytes(Path.Combine(Defines.GetExtensionsFolder(), selfName));
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
        HarmonyInstance.PatchAll();
        PluginReferenceHandler.ApplyPatch();
        HookProcessorPatch.ApplyPatch();
        ILVerifier.Init();
        Directory.CreateDirectory(RootDir);
    #if DEBUG
        Directory.CreateDirectory(Path.Combine(RootDir, "debug_gen"));
    #endif
        if (CCLEntrypoint.InitialConfig?.Development?.ReferenceAssemblies != null && CCLEntrypoint.InitialConfig.Development.DevMode && CCLEntrypoint.InitialConfig.Development.ReferenceAssemblies.Count > 0)
        {
            Directory.CreateDirectory(Path.Combine(RootDir, "ReferenceAssemblies"));
        }
        foreach (Type type in GetTypesWithoutError(Assembly.GetExecutingAssembly()).Where(x => typeof(BaseConverter).IsAssignableFrom(x) && !x.IsAbstract))
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
                    Logger.Error($"Failed to load {asmName} using {cv.Path}: \n{e}");
                }
            }
        }
    }
}