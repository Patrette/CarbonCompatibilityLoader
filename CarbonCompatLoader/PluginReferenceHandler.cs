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
    
    /*private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> op, ILGenerator ILGen)
    {
        List<CodeInstruction> IL = new(op);

        int tryStartBlock = -1;

        int bx = 0;
        CodeInstruction CIL = null;
        for (int index = 0; index < IL.Count; index++)
        {
            CIL = IL[index];
            if (CIL.blocks.Any(x => x.blockType == ExceptionBlockType.BeginExceptionBlock))
            {
                Logger.Info($"Found try block at: {index}");
                bx++;
                if (bx == 3)
                {
                    Logger.Info("Found try block 3!");
                    tryStartBlock = index;
                    break;
                }
            }
        }

        if (tryStartBlock == -1)
        {
            Logger.Error("Failed to find block?");
            return IL;
        }
        
        Logger.Info($"Block is: {CIL.opcode.ToString()} : {CIL.operand}");
        
        
        return IL;
    }*/

    internal static void SorryRaul(Assembly carbonMain)
    {
        System.Type SCT = carbonMain.GetType("Carbon.Jobs.ScriptCompilationThread");
        MainConverter.HarmonyInstance.Patch(
            AccessTools.Method(SCT, "_addReferences"), 
            postfix:new HarmonyMethod(AccessTools.Method(typeof(PluginReferenceHandler), nameof(ResolveCustomReference))));
        Logger.Warn("Patched carbonara");
    }
}