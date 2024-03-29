﻿using System.Reflection;
using AsmResolver;
using AsmResolver.DotNet.Serialized;
using AsmResolver.DotNet.Signatures.Types;
using CarbonCompatLoader.Converters;
using Facepunch;

namespace CarbonCompatLoader.Patches.Harmony;

public class HarmonyPatchProcessor : BaseHarmonyPatch
{
    public override void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info)
    {
        foreach (TypeDefinition td in asm.GetAllTypes())
        {
            bool invalid = false;
            List<CustomAttribute> patches = Pool.GetList<CustomAttribute>(); // who needs pooling
            foreach (CustomAttribute attr in td.CustomAttributes)
            {
                ITypeDefOrRef dc = attr.Constructor?.DeclaringType;
                if (dc == null) continue;
                CustomAttributeSignature sig = attr.Signature;
                if (sig == null) continue;
                if (dc.Name == "HarmonyPatch" && dc.Scope is SerializedAssemblyReference aref &&
                    aref.Name == HarmonyASM)
                {
                    if (sig.FixedArguments.Count > 1 && sig.FixedArguments[0].Element is TypeDefOrRefSignature tr &&
                        sig.FixedArguments[1].Element is Utf8String ats)
                    {
                        RegisterPatch(tr.DefinitionAssembly().Name, ats, tr.FullName, $"{asm.Assembly.Name} - {td.FullName}");
                        if (!PatchWhitelist.IsPatchAllowed(tr, ats))
                        {
                            Logger.Info($"Unpatching {td.FullName}::{ats}");
                            invalid = true;
                            patches.Add(attr);
                            break;
                        }
                    }
                    else
                    {
                        if (!PatchWhitelist.IsPatchAllowed(td, out string reason))
                        {
                            Logger.Info($"Unpatching {td.FullName} ({reason})");
                            invalid = true;
                            patches.Add(attr);
                            break;
                        }
                    }
                }
            }

            if (invalid)
            {
                td.CustomAttributes.Add(new CustomAttribute(asm.CorLibTypeFactory.CorLibScope.CreateTypeReference( // black magic
                    "System", "ObsoleteAttribute").CreateMemberReference(".ctor",
                    MethodSignature.CreateInstance(asm.CorLibTypeFactory.Void)).ImportWith(importer)));
                foreach (CustomAttribute attr in patches)
                {
                    td.CustomAttributes.Remove(attr);
                }
            }
        }
    }

    public static void RegisterPatch(string ASMName, string MethodName, string TypeName, string reason)
    {
        CurrentPatches.Add(new PatchInfoEntry(ASMName, MethodName, TypeName, reason));
        Logger.Info($"Found harmony patch {ASMName} - {TypeName}::{MethodName} from {reason}");
    }
    
    public class PatchInfoEntry
    {
        public string ASMName;
        public string TypeName;
        public string MethodName;
        public string reason;

        public PatchInfoEntry(string ASMName, string methodName, string typeName, string reason)
        {
            this.ASMName = ASMName;
            this.MethodName = methodName;
            this.TypeName = typeName;
            this.reason = reason;
        }
    }

    public static List<PatchInfoEntry> CurrentPatches = new();

    public static class PatchWhitelist
    {
        public static bool IsPatchAllowed(TypeDefOrRefSignature type, Utf8String method)
        {
            if (type.FullName == "ServerMgr" && type.Scope is SerializedAssemblyReference aref &&
                aref.Name == "Assembly-CSharp" && method == "UpdateServerInformation")
            {
                return false;
            }

            return true;
        }

        public static List<string> string_blacklist = new List<string>()
        {
            "Oxide.Core.OxideMod",
            "Oxide.Core"
        };

        public static bool IsPatchAllowed(TypeDefinition type, out string reason)
        {
            MethodDefinition target = type.Methods.FirstOrDefault(x => x.CustomAttributes.Any(y =>
                y.Constructor.DeclaringType.Name == "HarmonyTargetMethods" &&
                y.Constructor.DeclaringType.Scope is SerializedAssemblyReference asmref &&
                asmref.Name == BaseHarmonyPatch.HarmonyASM));
            if (target?.MethodBody is not CilMethodBody body) goto End;
            for (int index = 0; index < body.Instructions.Count; index++)
            {
                CilInstruction CIL = body.Instructions[index];
                if (CIL.OpCode == CilOpCodes.Ldstr && CIL.Operand is string str)
                {
                    if (string_blacklist.Contains(str))
                    {
                        reason = $"blacklisted string \"{str}\"";
                        return false;
                    }
                }
            }

            End:
            reason = null;
            return true;
        }
    }
}