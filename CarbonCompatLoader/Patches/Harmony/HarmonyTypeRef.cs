using AsmResolver.DotNet;
using CarbonCompatLoader.Converters;
using CarbonCompatLoader.Lib;

namespace CarbonCompatLoader.Patches.Harmony;

public class HarmonyTypeRef : BaseHarmonyPatch
{
    public TypeReference harmonyCompatRef = MainConverter.SelfModule.TopLevelTypes.First(x => 
        x.Namespace == "CarbonCompatLoader.Lib" && x.Name == nameof(HarmonyCompat)).ToTypeReference();
    public override void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info)
    {
        foreach (TypeReference tw in asm.GetImportedTypeReferences())
        {
            AssemblyReference aref = tw.Scope as AssemblyReference;
            if (aref != null && aref.Name == HarmonyASM)
            {
                if (tw.Namespace == Harmony1NS) tw.Namespace = Harmony2NS; // Namespace override
                if (tw.Name == "HarmonyInstance")
                {
                    tw.Name = "Harmony";
                }
            }
            if (aref != null && aref.Name == "Rust.Harmony")
            {
                tw.Namespace = $"CarbonCompatLoader.Lib";
                tw.Scope = (IResolutionScope)harmonyCompatRef.ImportWith(importer);
            }
        }
    }
}