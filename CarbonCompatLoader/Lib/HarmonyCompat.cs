using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CarbonCompatLoader.Lib;

public static class HarmonyCompat
{
    internal const string log = "[CHA] ";
    internal const string patch_str = log + "Patching method {0}::{1}";
    internal const string complete = log + "Patch complete\n";

    internal static class HarmonyLoader
    {
        internal class HarmonyMod
        {
            internal string Name { get; set; }

            internal string HarmonyId { get; set; }

            internal Assembly Assembly { get; set; }

            internal Type[] AllTypes { get; set; }

            internal List<IHarmonyModHooks> Hooks { get; } = new List<IHarmonyModHooks>();

        }
        internal static List<HarmonyLoader.HarmonyMod> loadedMods = new List<HarmonyLoader.HarmonyMod>();
    }

    internal struct HarmonyModInfo
    {
        internal string Name;

        internal string Version;
    }
    internal class OnHarmonyModLoadedArgs { }
    internal class OnHarmonyModUnloadedArgs { }

    public static void PatchProcessorCompat(Harmony instance, Type type, HarmonyMethod attributes)
    {
    #if DEBUG
        Debug.Log(log + $":START: Patching {type.FullName} using {instance.Id}\n\n");
    #endif
        //PatchProcessorCompat(null, null, null);
        MethodInfo[] methods = type.GetMethods();
        MethodInfo postfix = methods.FirstOrDefault(x=>x.GetCustomAttributes(typeof(HarmonyPostfix), false).Length > 0);
        MethodInfo prefix = methods.FirstOrDefault(x=>x.GetCustomAttributes(typeof(HarmonyPrefix), false).Length > 0);
        MethodInfo transpiler = methods.FirstOrDefault(x=>x.GetCustomAttributes(typeof(HarmonyTranspiler), false).Length > 0);
        MethodBase patchTargetMethod = methods.FirstOrDefault(x=>
            x.GetCustomAttributes(typeof(HarmonyTargetMethods), false).Length > 0 || 
            x.GetCustomAttributes(typeof(HarmonyTargetMethod), false).Length > 0);
        /*foreach (MethodInfo me in type.GetMethods())
        {
            if (postfix == null && me.GetCustomAttributes(typeof(HarmonyPostfix), false).Length > 0)
            {
                postfix = me;
            }
            else if (prefix == null && me.GetCustomAttributes(typeof(HarmonyPrefix), false).Length > 0)
            {
                prefix = me;
            }
            else if (transpiler == null && me.GetCustomAttributes(typeof(HarmonyTranspiler), false).Length > 0)
            {
                transpiler = me;
            }
            else if (patchTargetMethod == null &&
                     me.GetCustomAttributes(typeof(HarmonyTargetMethods), false).Length > 0 ||
                     me.GetCustomAttributes(typeof(HarmonyTargetMethod), false).Length > 0)
            {
                patchTargetMethod = me;
            }
        }*/
        //type.GetMethods().FirstOrDefault(m => m.GetCustomAttributes(typeof(HarmonyPostfix), false).Length > 0);
        //MethodBase prefix = type.GetMethods().FirstOrDefault(m => m.GetCustomAttributes(typeof(HarmonyPrefix), false).Length > 0);
        //MethodBase transpiler = type.GetMethods().FirstOrDefault(m => m.GetCustomAttributes(typeof(HarmonyTranspiler), false).Length > 0);

        //MethodBase patchTargetMethod = type.GetMethods().FirstOrDefault(m => m.GetCustomAttributes(typeof(HarmonyTargetMethods), false).Length > 0);
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
            if (methodsToPatch == null) return;
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

        if (methodsToPatch != null)
        {
            if (methodsToPatch.Count > 1) Debug.Log(log + $"Bulk patching {methodsToPatch.Count} methods");
            foreach (MethodBase original in methodsToPatch)
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
        }
        else if (single != null)
        {
        #if DEBUG
            Debug.Log(string.Format(patch_str, single.DeclaringType == null ? "NULL" : single.DeclaringType.FullName,
                single.Name));
        #endif
            PatchProcessor patcher = new PatchProcessor(instance, single);

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
    #if DEBUG
        Debug.Log(log + $":END: Patching {type.FullName} using {instance.Id}\n\n");
    #endif
        //MethodBase target = patchTargetMethod.Invoke(null, new []{instance});
        //return patcher;
    }
}