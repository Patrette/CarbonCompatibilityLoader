using System;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using CarbonCompatLoader.Converters;
using CarbonCompatLoader.Lib;
using HarmonyLib;

namespace CarbonCompatLoader.Patches.Harmony;

public class HarmonyILSwitch : BaseHarmonyPatch
{
    public override void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info)
    {
        IMethodDescriptor PatchProcessorCompatRef = importer.ImportMethod(AccessTools.Method(typeof(HarmonyCompat), nameof(HarmonyCompat.PatchProcessorCompat)));
        foreach (TypeDefinition td in asm.GetAllTypes())
        {
            foreach (MethodDefinition method in td.Methods)
            {
                if (!(method.MethodBody is CilMethodBody body)) continue;
                for (int index = 0; index < body.Instructions.Count; index++)
                {
                    CilInstruction CIL = body.Instructions[index];
                    // IL Patches
                    if (CIL.OpCode == CilOpCodes.Call && CIL.Operand is MemberReference { FullName: $"{Harmony2NS}.{HarmonyStr} {Harmony2NS}.{HarmonyStr}::Create(System.String)" })
                    {
                        CIL.OpCode = CilOpCodes.Newobj;
                        CIL.Operand = importer.ImportMethod(AccessTools.Constructor(typeof(HarmonyLib.Harmony), new Type[]{typeof(string)}));
                    }

                    if ((CIL.OpCode == CilOpCodes.Newobj) && CIL.Operand is MemberReference bref &&
                        bref.DeclaringType.DefinitionAssembly().Name == HarmonyASM &&
                        bref.DeclaringType.Name == "PatchProcessor" &&
                        bref.Name == ".ctor")
                    {
                        CIL.OpCode = CilOpCodes.Call;
                        CIL.Operand = PatchProcessorCompatRef;
                        continue;
                    }

                    if (CIL.OpCode == CilOpCodes.Callvirt && CIL.Operand is MemberReference cref &&
                        cref.DeclaringType.DefinitionAssembly().Name == HarmonyASM &&
                        cref.DeclaringType.Name == "PatchProcessor" &&
                        cref.Name == "Patch")
                    {
                        if (index != 0)
                        {
                            CilInstruction ccall = body.Instructions[index - 1];
                            if (ccall.OpCode == CilOpCodes.Call && ccall.Operand == PatchProcessorCompatRef)
                            {
                                body.Instructions.RemoveAt(index);
                                CilInstruction pop = body.Instructions[index];
                                if (pop.OpCode == CilOpCodes.Pop)
                                {
                                    body.Instructions.RemoveAt(index);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}