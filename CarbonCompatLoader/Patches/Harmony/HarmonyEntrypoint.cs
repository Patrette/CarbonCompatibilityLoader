using System;
using System.Collections.Generic;
using System.Linq;
using API.Events;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using CarbonCompatLoader.Converters;
using HarmonyLib;
using FieldAttributes = AsmResolver.PE.DotNet.Metadata.Tables.Rows.FieldAttributes;
using MethodAttributes = AsmResolver.PE.DotNet.Metadata.Tables.Rows.MethodAttributes;

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

        FieldDefinition loadedField = new FieldDefinition("loaded", FieldAttributes.PrivateScope, new FieldSignature(asm.CorLibTypeFactory.Boolean));
        
        int postHookIndex = 0;
        
        CodeGenHelpers.GenerateCarbonEventCall(load.CilMethodBody, importer, ref postHookIndex, CarbonEvent.HooksInstalled, postHookLoad, new CilInstruction(CilOpCodes.Ldarg_0));
        
        load.CilMethodBody.Instructions.Add(new CilInstruction(CilOpCodes.Ret));
        CilInstruction postHookRet = new CilInstruction(CilOpCodes.Ret);
        postHookLoad.CilMethodBody.Instructions.AddRange(new CilInstruction[]
        {
            // load check
            new CilInstruction(CilOpCodes.Ldarg_0),
            new CilInstruction(CilOpCodes.Ldfld, loadedField),
            new CilInstruction(CilOpCodes.Brtrue_S, postHookRet.CreateLabel()),
            new CilInstruction(CilOpCodes.Ldarg_0),
            new CilInstruction(CilOpCodes.Ldc_I4_1),
            new CilInstruction(CilOpCodes.Stfld, loadedField),
            
            // harmony patch all
            new CilInstruction(CilOpCodes.Ldstr, $"__CCL:{asm.Assembly.Name}:{guid:N}"),
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
        postHookLoad.CilMethodBody.Instructions.Add(postHookRet);
        entryDef.Methods.Add(postHookLoad);
        entryDef.Fields.Add(loadedField);
    }
}