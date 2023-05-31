using AsmResolver.DotNet;
using CarbonCompatLoader.Converters;

namespace CarbonCompatLoader.Patches.Oxide;

public abstract class BaseOxidePatch : IASMPatch
{
    
    public abstract void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info);
}