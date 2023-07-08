using API.Events;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using CarbonCompatLoader.Converters;
using FieldAttributes = AsmResolver.PE.DotNet.Metadata.Tables.Rows.FieldAttributes;
using MethodAttributes = AsmResolver.PE.DotNet.Metadata.Tables.Rows.MethodAttributes;

namespace CarbonCompatLoader.Patches.Oxide;

public class OxideEntrypoint : BaseOxidePatch
{
    public override void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info)
    {
        Guid guid = Guid.NewGuid();
        List<TypeDefinition> entryPoints = asm.GetAllTypes().Where(x=>x.BaseType?.FullName == "Oxide.Core.Extensions.Extension" && x.BaseType.DefinitionAssembly().Name == "Carbon.Common").ToList();
        if (entryPoints.Count == 0)
        {
            info.noEntryPoint = true;
            return;
        }
        
        info.author ??= entryPoints[0].Properties.FirstOrDefault(x => x.Name == "Author" && x.GetMethod is { IsVirtual: true })?.GetMethod?.CilMethodBody?.Instructions.FirstOrDefault(x => x.OpCode == CilOpCodes.Ldstr)?.Operand as string;

        CodeGenHelpers.GenerateEntrypoint(asm, importer, OxideStr, guid, out MethodDefinition load, out TypeDefinition entryDef);
        load.CilMethodBody = new CilMethodBody(load);
        
        MethodDefinition serverInit = new MethodDefinition("serverInit",
            MethodAttributes.CompilerControlled, MethodSignature.CreateInstance(asm.CorLibTypeFactory.Void, importer.ImportTypeSignature(typeof(EventArgs))));
        serverInit.CilMethodBody = new CilMethodBody(serverInit);
        
        FieldDefinition loadedField = new FieldDefinition("loaded", FieldAttributes.PrivateScope, new FieldSignature(asm.CorLibTypeFactory.Boolean));
        
        int postHookIndex = 0;
        
        CodeGenHelpers.GenerateCarbonEventCall(load.CilMethodBody, importer, ref postHookIndex, CarbonEvent.HookValidatorRefreshed, serverInit, new CilInstruction(CilOpCodes.Ldarg_0));
        
        load.CilMethodBody.Instructions.Add(new CilInstruction(CilOpCodes.Ret));
        
        CilInstruction postHookRet = new CilInstruction(CilOpCodes.Ret);
        serverInit.CilMethodBody.Instructions.AddRange(new CilInstruction[]
        {
            // load check
            new CilInstruction(CilOpCodes.Ldarg_0),
            new CilInstruction(CilOpCodes.Ldfld, loadedField),
            new CilInstruction(CilOpCodes.Brtrue_S, postHookRet.CreateLabel()),
            new CilInstruction(CilOpCodes.Ldarg_0),
            new CilInstruction(CilOpCodes.Ldc_I4_1),
            new CilInstruction(CilOpCodes.Stfld, loadedField)

        });
        
        foreach (TypeDefinition entry in entryPoints)
        {
            MethodDefinition extLoadMethod = entry.Methods.FirstOrDefault(x => x.Name == "Load" && x.IsVirtual);
            MethodDefinition extCtor = entry.Methods.FirstOrDefault(x => x.Name == ".ctor" && x.Parameters.Count == 1);
            if (extLoadMethod == null) continue;
            serverInit.CilMethodBody.Instructions.AddRange(new CilInstruction[]
            {
                new CilInstruction(CilOpCodes.Ldnull),
                new CilInstruction(CilOpCodes.Newobj, extCtor),
                new CilInstruction(CilOpCodes.Callvirt, extLoadMethod)
            });
        }
        serverInit.CilMethodBody.Instructions.Add(postHookRet);
        entryDef.Fields.Add(loadedField);
        entryDef.Methods.Add(serverInit);
    }
}