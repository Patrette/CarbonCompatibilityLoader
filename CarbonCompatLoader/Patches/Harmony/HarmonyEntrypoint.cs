using System;
using System.Collections.Generic;
using System.Linq;
using API.Events;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using CarbonCompatLoader.Converters;
using HarmonyLib;

namespace CarbonCompatLoader.Patches.Harmony;

public class HarmonyEntrypoint : BaseHarmonyPatch
{
    public override void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info)
    {
        Guid guid = Guid.NewGuid();
        List<TypeDefinition> entryPoints = asm.GetAllTypes().Where(x => x.Interfaces.Any(y=>y.Interface?.FullName == "CarbonCompatLoader.Lib.HarmonyCompat+IHarmonyModHooks")).ToList();
        CodeGenHelpers.GenerateEntrypoint(asm, importer, HarmonyStr, guid, out MethodDefinition load, out TypeDefinition entryDef);
        load.CilMethodBody = new CilMethodBody(load);

        MethodDefinition postHookLoad = new MethodDefinition("postHookLoad",
            MethodAttributes.CompilerControlled, MethodSignature.CreateInstance(asm.CorLibTypeFactory.Void, importer.ImportTypeSignature(typeof(EventArgs))));
        postHookLoad.CilMethodBody = new CilMethodBody(postHookLoad);

        int postHookIndex = 0;
        
        CodeGenHelpers.GenerateCarbonEventSubscribe(load.CilMethodBody, importer, ref postHookIndex, CarbonEvent.HooksInstalled, postHookLoad, new CilInstruction(CilOpCodes.Ldarg_0));
        
        load.CilMethodBody.Instructions.Add(new CilInstruction(CilOpCodes.Ret));
        
        postHookLoad.CilMethodBody.Instructions.AddRange(new CilInstruction[]
        {
            new CilInstruction(CilOpCodes.Ldstr, $"__CCL:{guid:N}"),
            new CilInstruction(CilOpCodes.Newobj, importer.ImportMethod(AccessTools.Constructor(typeof(HarmonyLib.Harmony), new Type[]{typeof(string)}))),
            new CilInstruction(CilOpCodes.Callvirt, importer.ImportMethod(AccessTools.Method(typeof(HarmonyLib.Harmony), "PatchAll"))) 
        });

        if (entryPoints.Count > 0)
        {
            List<KeyValuePair<TypeDefinition, List<MethodDefinition>>> input =
                new List<KeyValuePair<TypeDefinition, List<MethodDefinition>>>();
            foreach (TypeDefinition entry in entryPoints)
            {
                input.Add(new KeyValuePair<TypeDefinition, List<MethodDefinition>>(entry, new List<MethodDefinition>{entry.Methods.First(x=>x.Name == "OnLoaded")}));
            }

            int multiCallIndex = postHookLoad.CilMethodBody.Instructions.Count;
            CodeGenHelpers.DoMultiMethodCall(postHookLoad.CilMethodBody, ref multiCallIndex, null, input);
        }
        postHookLoad.CilMethodBody.Instructions.Add(new CilInstruction(CilOpCodes.Ret));
        entryDef.Methods.Add(postHookLoad);
    }
}