using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using CarbonCompatLoader.Converters;
using HarmonyLib;
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
        CodeGenHelpers.GenerateEntrypoint(asm, importer, OxideStr, guid, out MethodDefinition load, out TypeDefinition entryDef);
        load.CilMethodBody = new CilMethodBody(load);
        foreach (TypeDefinition entry in entryPoints)
        {
            MethodDefinition extLoadMethod = entry.Methods.FirstOrDefault(x => x.Name == "Load" && x.IsVirtual);
            MethodDefinition extCtor = entry.Methods.FirstOrDefault(x => x.Name == ".ctor" && x.Parameters.Count == 1);
            if (extLoadMethod == null) continue;
            load.CilMethodBody.Instructions.AddRange(new CilInstruction[]
            {
                new CilInstruction(CilOpCodes.Ldnull),
                new CilInstruction(CilOpCodes.Newobj, extCtor),
                new CilInstruction(CilOpCodes.Callvirt, extLoadMethod)
            });
        }
        load.CilMethodBody.Instructions.Add(new CilInstruction(CilOpCodes.Ret));
    }
}