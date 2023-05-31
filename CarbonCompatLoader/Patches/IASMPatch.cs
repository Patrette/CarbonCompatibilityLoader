using AsmResolver.DotNet;
using CarbonCompatLoader.Converters;

namespace CarbonCompatLoader.Patches;

public interface IASMPatch
{
    public abstract void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info);
}