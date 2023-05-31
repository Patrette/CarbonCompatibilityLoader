using AsmResolver.DotNet;
using CarbonCompatLoader.Converters;
using CarbonCompatLoader.Patches;

namespace CarbonCompatLoader.CodeGen;

public abstract class CodeGenTemplate : IASMPatch
{
    public abstract void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info);
}