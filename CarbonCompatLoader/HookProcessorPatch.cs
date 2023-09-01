using System.Reflection;
using System.Reflection.Emit;
using API.Events;
using API.Hooks;
using Carbon;
using CarbonCompatLoader.Lib;
using CarbonCompatLoader.Patches.Harmony;
using HarmonyLib;
using TypeInfo = System.Reflection.TypeInfo;

namespace CarbonCompatLoader;

internal static class HookProcessorPatch
{
    public static bool InitialHooksInstalled;
    /*private static void HookExCTOR(IHook __instance)
    {
        if (__instance.TargetMethods.Count == 0) return;
        string asmName = __instance.TargetMethods[0].DeclaringType.Assembly.GetName().Name;
        string typeName = __instance.TargetMethods[0].DeclaringType.FullName;
        string methodName = __instance.TargetMethods[0].Name;
        //Logger.Info($"Found HOOKY {asmName} - {typeName}::{methodName}");
        HarmonyPatchProcessor.PatchInfoEntry patchInfo = HarmonyPatchProcessor.CurrentPatches.FirstOrDefault(x =>
            x.ASMName == asmName && x.TypeName == typeName && x.MethodName == methodName);
        if (patchInfo != null)
        {
        #if DEBUG
            Logger.Info($"{patchInfo.reason} Forcing hook {__instance.TargetMethods[0]} to static");
        #endif
            if ((__instance.Options & HookFlags.Patch) != HookFlags.Patch)
                __instance.Options |= HookFlags.Static;
        }

    }*/
    
    public static void HookReload(EventArgs args = null)
    {
        Logger.Info("Processing dynamic hooks");
        foreach (IHook Hooky in Community.Runtime.HookManager.LoadedDynamicHooks)
        {
            //Logger.Info($"Found dyn hooky: {Hooky.HookFullName}");
            if (Hooky.TargetMethods.Count == 0) return;
            MethodBase cache = Hooky.TargetMethods[0];
            string asmName = cache.DeclaringType.Assembly.GetName().Name;
            string typeName = cache.DeclaringType.FullName;
            string methodName = cache.Name;
            //Logger.Info($"Found HOOKY {asmName} - {typeName}::{methodName}");
            HarmonyPatchProcessor.PatchInfoEntry patchInfo = HarmonyPatchProcessor.CurrentPatches.FirstOrDefault(x =>
                x.ASMName == asmName && x.TypeName == typeName && x.MethodName == methodName);
            if (patchInfo != null)
            {
            #if DEBUG
                Logger.Info($"{patchInfo.reason} Forcing hook {Hooky.TargetMethods[0]} to static");
            #endif
                if ((Hooky.Options & HookFlags.Patch) != HookFlags.Patch)
                    Community.Runtime.HookManager.Subscribe(Hooky.Identifier, "CCL.Static");
            }
        }

        ForceUpdateHooks();
    }

    private static bool _hooks_called = false;

    public static void HooksInstalled()
    {
        if (_hooks_called) return;
        _hooks_called = true;
        Logger.Info("Initial hooks installed");
        Community.Runtime.Events.Trigger(HarmonyCompat.InitialHookInstallationComplete, EventArgs.Empty);
    }
    
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> op, ILGenerator ILGen)
    {
        FieldInfo field = AccessTools.Field(typeof(HookProcessorPatch), nameof(InitialHooksInstalled));
        List<CodeInstruction> IL = new(op);
        int LFIdx = IL.FindIndex(
            x => x.opcode == OpCodes.Ldfld && x.operand is FieldInfo { Name: "PatchLimitPerCycle" });
        Label maxVLabel = ILGen.DefineLabel();
        Label setLimitLabel = ILGen.DefineLabel();
        CodeInstruction initialCode = new CodeInstruction(OpCodes.Ldsfld, field);
        Label initBranch = (Label)IL.Find(x => x.opcode == OpCodes.Brfalse_S).operand;
        IL.First(x => x.labels.Contains(initBranch)).MoveLabelsTo(initialCode);
        IL.InsertRange(LFIdx-1, new CodeInstruction[]
        {
            initialCode,
            new CodeInstruction(OpCodes.Brfalse_S, maxVLabel),
        });
        CodeInstruction ldcI = new CodeInstruction(OpCodes.Ldc_I4, Int32.MaxValue);
        ldcI.labels.Add(maxVLabel);
        IL.InsertRange(LFIdx+3, new CodeInstruction[]
        {
            new CodeInstruction(OpCodes.Br_S, setLimitLabel),
            ldcI
        });
        IL.Find(x=>x.opcode == OpCodes.Stloc_0).labels.Add(setLimitLabel);
        Label ret = ILGen.DefineLabel();
        IL[^1].labels.Add(ret);
        IL.InsertRange(IL.Count-1, new CodeInstruction[]
        {
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(MainConverter.CarbonMain.GetType("Carbon.Hooks.PatchManager"), "_workQueue")),
            new CodeInstruction(OpCodes.Callvirt, typeof(Queue<string>).GetMethod("get_Count")),
            new CodeInstruction(OpCodes.Brtrue_S, ret),
            new CodeInstruction(OpCodes.Ldsfld, field),
            new CodeInstruction(OpCodes.Brtrue_S, ret),
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Stsfld, field),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HookProcessorPatch), nameof(HooksInstalled)))
        });
        return IL;
    }

    private static Type patchManagerType;

    private static MethodInfo hookUpdateMethod = AccessTools.Method(patchManagerType, "Update");

    internal static void ForceUpdateHooks(bool noLimit = true)
    {
        if (noLimit) InitialHooksInstalled = false;
        hookUpdateMethod.Invoke(Carbon.Community.Runtime.HookManager, Array.Empty<object>());
    }

    internal static void ApplyPatch()
    {
        patchManagerType = MainConverter.CarbonMain.GetType("Carbon.Hooks.PatchManager");
        Community.Runtime.Events.Subscribe(CarbonEvent.HooksInstalled, HookReload);
        /*MainConverter.HarmonyInstance.Patch(
            AccessTools.Constructor(MainConverter.CarbonMain.GetType("Carbon.Hooks.HookEx"), new[] { typeof(TypeInfo) }), 
            postfix:new HarmonyMethod(AccessTools.Method(typeof(HookProcessorPatch), nameof(HookExCTOR))));*/
        
        MainConverter.HarmonyInstance.Patch(
            AccessTools.Method(patchManagerType, "Update"), 
            transpiler:new HarmonyMethod(AccessTools.Method(typeof(HookProcessorPatch), nameof(Transpiler))));
    #if DEBUG
        Logger.Warn("Patched HookExCTOR");
    #endif
    }
}