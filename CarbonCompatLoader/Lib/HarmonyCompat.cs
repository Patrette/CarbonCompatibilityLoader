using System.Reflection;
using API.Events;
using API.Hooks;
using Carbon;
using CarbonCompatLoader.Patches.Harmony;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace CarbonCompatLoader.Lib;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class HarmonyCompat
{
    public const CarbonEvent InitialHookInstallationComplete = (CarbonEvent)1337;
    
    public const CarbonEvent PostInitialHookInstallationComplete = (CarbonEvent)2337;
    
    internal const string log = "[CHA] ";
    internal const string patch_str = log + "Patching method {0}::{1}";
    internal const string complete = log + "Patch complete\n";

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    internal static class HarmonyLoader
    {
        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        internal class HarmonyMod
        {
            internal string Name { get; set; }

            internal string HarmonyId { get; set; }

            public Harmony Harmony { get; set; }

            internal Assembly Assembly { get; set; }

            internal Type[] AllTypes { get; set; }

            internal List<IHarmonyModHooks> Hooks { get; } = new List<IHarmonyModHooks>();
        }

        internal static List<HarmonyLoader.HarmonyMod> loadedMods = new List<HarmonyLoader.HarmonyMod>();
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    internal struct HarmonyModInfo
    {
        internal string Name;

        internal string Version;
    }

    internal class OnHarmonyModLoadedArgs
    {
    }

    internal class OnHarmonyModUnloadedArgs
    {
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    internal interface IHarmonyModHooks
    {
        void OnLoaded(OnHarmonyModLoadedArgs args);

        void OnUnloaded(OnHarmonyModUnloadedArgs args);
    }

    public static void PatchProcessorCompat(Harmony instance, Type type, HarmonyMethod attributes)
    {
    #if DEBUG
        Debug.Log(log + $":START: Patching {type.FullName} using {instance.Id}\n\n");
    #endif
        //PatchProcessorCompat(null, null, null);
        MethodInfo[] methods = type.GetMethods();
        MethodInfo postfix =
            methods.FirstOrDefault(x => x.GetCustomAttributes(typeof(HarmonyPostfix), false).Length > 0);
        MethodInfo prefix = methods.FirstOrDefault(x => x.GetCustomAttributes(typeof(HarmonyPrefix), false).Length > 0);
        MethodInfo transpiler =
            methods.FirstOrDefault(x => x.GetCustomAttributes(typeof(HarmonyTranspiler), false).Length > 0);
        MethodBase patchTargetMethod = methods.FirstOrDefault(x =>
            x.GetCustomAttributes(typeof(HarmonyTargetMethods), false).Length > 0 ||
            x.GetCustomAttributes(typeof(HarmonyTargetMethod), false).Length > 0);
        if (patchTargetMethod == null)
        {
            throw new NullReferenceException($"failed to find target method in {type.FullName}");
        }

        List<MethodBase> methodsToPatch = null;
        MethodBase single = null;
        if (((MethodInfo)patchTargetMethod).ReturnType == typeof(IEnumerable<MethodBase>))
        {
            methodsToPatch = ((IEnumerable<MethodBase>)patchTargetMethod.Invoke(null,
                patchTargetMethod.GetParameters().Length > 0 ? new object[] { null } : Array.Empty<object>())).ToList();
        }
        else if (((MethodInfo)patchTargetMethod).ReturnType == typeof(MethodBase))
        {
            single = (MethodBase)patchTargetMethod.Invoke(null,
                patchTargetMethod.GetParameters().Length > 0 ? new object[] { null } : Array.Empty<object>());
        }
        else
        {
            return;
        }

        //if (methodsToPatch == null && single == null)
        {
            //return;
        }

        void ProcessType(MethodBase original, bool pregen)
        {
            if (pregen)
            {
                Logger.Info($"PreGen for {original}");
                HarmonyPatchProcessor.RegisterPatch(original.DeclaringType.Assembly.GetName().Name, original.Name, original.DeclaringType.FullName, $"{type.Assembly.GetName().Name} - {type.FullName}");
                return;
            }
            Logger.Info($"Patching {original}");
            try
            {
            #if DEBUG
                Debug.Log(string.Format(patch_str,
                    original.DeclaringType == null ? "NULL" : original.DeclaringType.FullName, original.Name));
            #endif
                PatchProcessor patcher = new PatchProcessor(instance, original);

                if (postfix != null)
                {
                #if DEBUG
                    Debug.Log(log + $"> postfix");
                #endif
                    patcher.AddPostfix(postfix);
                }

                if (prefix != null)
                {
                #if DEBUG
                    Debug.Log(log + $"> prefix");
                #endif
                    patcher.AddPrefix(prefix);
                }

                if (transpiler != null)
                {
                #if DEBUG
                    Debug.Log(log + $"> transpiler");
                #endif
                    patcher.AddTranspiler(transpiler);
                }

                patcher.Patch();

            #if DEBUG
                Debug.Log(complete);
            #endif
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to patch {original.Name}");
            }
        }

        bool pregen = true;
        
        loop:

        if (methodsToPatch != null)
        {
            if (methodsToPatch.Count > 1 && !pregen) Debug.Log(log + $"Bulk patching {methodsToPatch.Count} methods");
            foreach (MethodBase original in methodsToPatch)
            {
                ProcessType(original, pregen);
            }
        }
        else if (single != null)
        {
            ProcessType(single, pregen);
        }

        if (pregen)
        {
            pregen = false;
            HookProcessorPatch.HookReload();
            goto loop;
        }
    #if DEBUG
        Debug.Log(log + $":END: Patching {type.FullName} using {instance.Id}\n\n");
    #endif
        //MethodBase target = patchTargetMethod.Invoke(null, new []{instance});
        //return patcher;
    }
}