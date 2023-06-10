using System.Reflection;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using CarbonCompatLoader.Converters;
using CarbonCompatLoader.Lib;
using HarmonyLib;

namespace CarbonCompatLoader.Patches.Oxide;

public class OxideILSwitch : BaseOxidePatch
{
    private static MethodInfo pluginLoaderMethod = AccessTools.Method(typeof(OxideCompat), "RegisterPluginLoader");
    private static MethodInfo consoleCommand1 = AccessTools.Method(typeof(OxideCompat), "AddConsoleCommand1");
    private static MethodInfo chatCommand1 = AccessTools.Method(typeof(OxideCompat), "AddChatCommand1");
    private static MethodInfo GetExtensionDirectory = AccessTools.Method(typeof(OxideCompat), nameof(OxideCompat.GetExtensionDirectory));
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

                    // plugin loader
                    if (CIL.OpCode == CilOpCodes.Callvirt && 
                        CIL.Operand is MemberReference aref && 
                        aref.Name == "RegisterPluginLoader" && 
                        aref.Parent is TypeReference atw && 
                        atw.DefinitionAssembly().Name == MainConverter.Common.Name)
                    {
                        CIL.OpCode = CilOpCodes.Call;
                        CIL.Operand = importer.ImportMethod(pluginLoaderMethod);
                        continue;
                    }
                    // add console command
                    if (CIL.OpCode == CilOpCodes.Callvirt && 
                        CIL.Operand is MemberReference bref && 
                        bref.Name == "AddConsoleCommand" && 
                        bref.Parent is TypeReference btw && 
                        btw.FullName == "Oxide.Game.Rust.Libraries.Command" &&
                        bref.Signature is MethodSignature asig &&
                        asig.ParameterTypes.Count == 3 &&
                        asig.ParameterTypes[0].ElementType == ElementType.String &&
                        asig.ParameterTypes[1].FullName == "Oxide.Core.Plugins.Plugin" &&
                        asig.ParameterTypes[2].FullName == "System.Func`2<ConsoleSystem+Arg, System.Boolean>" &&
                        btw.DefinitionAssembly().Name == MainConverter.Common.Name)
                    {
                        CIL.OpCode = CilOpCodes.Call;
                        CIL.Operand = importer.ImportMethod(consoleCommand1);
                        continue;
                    }
                    // add chat command
                    if (CIL.OpCode == CilOpCodes.Callvirt && 
                        CIL.Operand is MemberReference cref && 
                        cref.Name == "AddChatCommand" && 
                        cref.Parent is TypeReference ctw && 
                        ctw.FullName == "Oxide.Game.Rust.Libraries.Command" &&
                        cref.Signature is MethodSignature bsig &&
                        bsig.ParameterTypes.Count == 3 &&
                        bsig.ParameterTypes[0].ElementType == ElementType.String &&
                        bsig.ParameterTypes[1].FullName == "Oxide.Core.Plugins.Plugin" &&
                        ctw.DefinitionAssembly().Name == MainConverter.Common.Name)
                    {
                        switch (bsig.ParameterTypes[2].FullName)
                        {
                            case "System.Action`3<BasePlayer, System.String, System.String[]>":
                                CIL.Operand = importer.ImportMethod(chatCommand1);
                                goto cend;
                            default: 
                                continue;
                        }
                        cend:
                        CIL.OpCode = CilOpCodes.Call;
                        continue;
                    }
                    
                    if (CIL.OpCode == CilOpCodes.Callvirt && 
                        CIL.Operand is MemberReference dref && 
                        dref.Name == "RegisterLibrary" && 
                        dref.Parent is TypeReference dtw && 
                        dtw.FullName == "Oxide.Core.Extensions.ExtensionManager" &&
                        dtw.DefinitionAssembly().Name == MainConverter.Common.Name)
                    {
                        CIL.OpCode = CilOpCodes.Pop;
                        CIL.Operand = null;
                        body.Instructions.InsertRange(index, new CilInstruction[]
                        {
                            new CilInstruction(CilOpCodes.Pop),
                            new CilInstruction(CilOpCodes.Pop)
                        });
                        index+=2;
                        continue;
                    }
                    
                    //extension paths
                    if (CIL.OpCode == CilOpCodes.Callvirt && 
                        CIL.Operand is MemberReference eref && 
                        eref.Name == "get_ExtensionDirectory" && 
                        eref.Parent is TypeReference etw &&
                        etw.FullName == "Oxide.Core.OxideMod" &&
                        etw.DefinitionAssembly().Name == MainConverter.Common.Name)
                    {
                        CIL.OpCode = CilOpCodes.Call;
                        CIL.Operand = importer.ImportMethod(GetExtensionDirectory);
                        continue;
                    }
                }
            }
        }
    }
}