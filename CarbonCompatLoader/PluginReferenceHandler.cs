using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Carbon.Jobs;
using HarmonyLib;
using Microsoft.CodeAnalysis;

namespace CarbonCompatLoader;

internal static class PluginReferenceHandler
{
    internal static readonly Dictionary<string, PortableExecutableReference> RefCache = new Dictionary<string, PortableExecutableReference>();
    
    private static void ResolveCustomReference(List<MetadataReference> __result)
    {
        __result?.AddRange(RefCache.Values);
    }

    internal static void SorryRaul(Assembly carbonMain)
    {
        System.Type SCT = carbonMain.GetType("Carbon.Jobs.ScriptCompilationThread");
        MainConverter.HarmonyInstance.Patch(
            AccessTools.Method(SCT, "_addReferences"), 
            postfix:new HarmonyMethod(AccessTools.Method(typeof(PluginReferenceHandler), nameof(ResolveCustomReference))));
        Logger.Warn("Patched carbonara");
    }
}