using CarbonCompatLoader.Converters;

namespace CarbonCompatLoader.Patches.Harmony;

public abstract class BaseHarmonyPatch : IASMPatch
{
    public const string HarmonyASM = "0Harmony";
    public const string Harmony1NS = HarmonyStr;
    public const string Harmony2NS = "HarmonyLib";
    public const string HarmonyStr = "Harmony";
    public abstract void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info);
}