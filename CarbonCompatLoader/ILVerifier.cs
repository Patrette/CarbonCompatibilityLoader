using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using AsmResolver.DotNet.Builder;
using AsmResolver.PE.DotNet.Metadata.Tables;
using Carbon.Core;
using ILVerify;
using K4os.Compression.LZ4.Streams;
using Newtonsoft.Json;
using MethodDefinition = AsmResolver.DotNet.MethodDefinition;
using ModuleDefinition = AsmResolver.DotNet.ModuleDefinition;

namespace CarbonCompatLoader;

public static unsafe class ILVerifier
{
    /*private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> op, ILGenerator ILGen)
    {
        List<CodeInstruction> IL = new(op);
        int idx = IL.FindIndex(x => x.opcode == OpCodes.Stloc_0) + 1;
        Logger.Info($"IDX: {idx}");
        IL.InsertRange(idx, new CodeInstruction[]
        {
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Exception), nameof(Exception.ToString))),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Logger), nameof(Logger.Info)))
        });
        return IL;
    }*/

    public static List<VerificationResult> ProcessResults(VerificationResult[] original, VerificationResult[] converted, ModuleDefinition asm, TokenMapping mappings)
    {
        List<VerificationResult> results = new List<VerificationResult>();
        for (int X = 0; X < converted.Length; X++)
        {
            VerificationResult CV = converted[X];
            MethodDefinition method = mappings.GetMethodByToken(new MetadataToken(TableIndex.Method, (uint)CV.Method.GetHashCode()));
            //Logger.Info($"{method.FullName} - {CV.Code.ToString()} - {CV.ExceptionID.ToString()} - {CV.Message}");

            for (int Y = 0; Y < original.Length; Y++)
            {
                VerificationResult OR = original[Y];
                if (OR.Code == CV.Code && OR.ExceptionID == CV.ExceptionID && OR.Method.GetHashCode() == method.MetadataToken.Rid)
                {
                    goto end;
                }
            }
            
            Logger.Error($"IL Verification error ({CV.Code.ToString()}-{CV.ExceptionID.ToString()}) : {method.FullName} - {CV.Message}");
            
            end:;
        }

        return results;
    }

    public static VerificationResult[] VerifyAssembly(byte[] raw, bool ne = false)
    {
        fixed (byte* be = raw)
        {
            using (PEReader reader = new(be, raw.Length))
            {
                MetadataReader meta = reader.GetMetadataReader();
                //List<VerificationResult> info = verifier.Verify(reader).Where(x=>x.Code != VerifierError.InitOnly).ToList();
                //Logger.Info($"Found {info.Count} errors");
                VerificationResult[] sorbano = verifier.Verify(reader).ToArray();

                return sorbano;
            }
        }
    }

    private static Verifier verifier;

    private static Dictionary<string, string> externalRefManifest;
    public static void Init()
    {
        Logger.Info("IL Init");
        
        /*MainConverter.HarmonyInstance.Patch(
            AccessTools.Method(typeof(Verifier).GetNestedType("<>c__DisplayClass12_0", BindingFlags.Instance | BindingFlags.NonPublic), "<VerifyMethod>g__reportException|0"), 
            transpiler:new HarmonyMethod(AccessTools.Method(typeof(ILVerifier), nameof(Transpiler))));*/
        
        using (StreamReader reader = new StreamReader(typeof(ILVerifier).Assembly.GetManifestResourceStream("CarbonCompatLoader.ExternalRefs.manifest.json")))
        {
            externalRefManifest = JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());
        }
        verifier = new Verifier(new Resolver(),
            new VerifierOptions() { IncludeMetadataTokensInErrorMessages = true, SanityChecks = true });
        verifier.SetSystemModuleName(new AssemblyName("mscorlib"));
    }

    private static string GetASMName(this AssemblyName name)
    {
        string asmName = name.Name;
        if (asmName == "0Harmony")
        {
            asmName += $"-{name.Version.Major}";
        }

        return asmName;
    }

    private static string CASMName = typeof(ILVerifier).Assembly.GetName().Name;
    private class Resolver : IResolver
    {
        private Dictionary<string, PEReader> cache = new();
        public PEReader ResolveAssembly(AssemblyName assemblyName)
        {
            string asmName = assemblyName.GetASMName();
            //Logger.Info($"Attempting to resolve ({asmName})");
            if (cache.TryGetValue(asmName, out PEReader reader))
            {
                return reader;
            }

            if (externalRefManifest.TryGetValue(asmName, out string extName))
            {
                //Logger.Info($"Found ext: {extName}");
                MemoryStream ms = new MemoryStream();
                using (LZ4DecoderStream lzdc = LZ4Stream.Decode(
                           typeof(ILVerifier).Assembly.GetManifestResourceStream(
                               $"CarbonCompatLoader.ExternalRefs.{extName}"),
                           interactive: true))
                {

                    lzdc.CopyTo(ms);
                }
                ms.Position = 0;
                return cache[asmName] = new PEReader(ms);
            }
            Assembly loaded = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(x => x.GetName().GetASMName() == asmName && !string.IsNullOrWhiteSpace(x.Location));
            if (loaded != null)
            {
                return getPEReader(loaded.Location);
            }

            string fileName = assemblyName.Name + ".dll";
            
            string path = Path.Combine(Defines.GetRustManagedFolder(), fileName);
            if (File.Exists(path))
            {
                if (asmName != "0Harmony-2")
                    return getPEReader(path);
            }
            path = Path.Combine(Defines.GetLibFolder(), fileName);
            if (File.Exists(path))
            {
                if (asmName != "0Harmony-1")
                    return getPEReader(path);
            }
            path = Path.Combine(Defines.GetManagedFolder(), fileName);
            if (File.Exists(path))
            {
                return getPEReader(path);
            }

            if (asmName == CASMName)
            {
                MemoryStream ms = new MemoryStream(MainConverter.ThisASM);
                return new PEReader(ms);
            }
            
            PEReader getPEReader(string asm)
            {
                //Logger.Info($"resolving {asm} - {asmName}");
                return cache[asmName] = new PEReader(new MemoryStream(File.ReadAllBytes(asm)));
            }

            Logger.Error($"Failed to resolve ({asmName})");
            throw new NotImplementedException();
        }
        public PEReader ResolveModule(AssemblyName referencingAssembly, string fileName)
        {
            throw new NotImplementedException();
        }
    }
}