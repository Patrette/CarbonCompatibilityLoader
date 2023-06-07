using System.Reflection;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using CarbonCompatLoader.Converters;
using CarbonCompatLoader.Lib;
using HarmonyLib;

namespace CarbonCompatLoader.Patches.Oxide;

public class OxideILSwitch : BaseOxidePatch
{
    private static MethodInfo pluginLoaderMethod = AccessTools.Method(typeof(OxideCompat), "RegisterPluginLoader");
    public override void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info)
    {
        foreach (TypeDefinition td in asm.GetAllTypes())
        {
            foreach (MethodDefinition method in td.Methods)
            {
                if (!(method.MethodBody is CilMethodBody body)) continue;
                for (int index = 0; index < body.Instructions.Count; index++)
                {
                    CilInstruction CIL = body.Instructions[index];
                    // IL Patches

                    if (CIL.OpCode == CilOpCodes.Callvirt && 
                        CIL.Operand is MemberReference mref && 
                        mref.Name == "RegisterPluginLoader" && 
                        mref.Parent is TypeReference tw && 
                        tw.DefinitionAssembly().Name == MainConverter.Common.Name)
                    {
                        CIL.OpCode = CilOpCodes.Call;
                        CIL.Operand = importer.ImportMethod(pluginLoaderMethod);
                    }
                }
            }
        }
    }
}