using System.Collections.Generic;
using AsmResolver.DotNet;
using CarbonCompatLoader.Patches;
using CarbonCompatLoader.Patches.Harmony;

namespace CarbonCompatLoader.Converters;

public class HarmonyConverter : BaseConverter
{
    public override List<IASMPatch> patches => new()
    {
        new HarmonyTypeRef(),
        new HarmonyILSwitch(),
        new HarmonyBlacklist(),
        
        new AssemblyVersionPatch()
    };
    public override string Path => "harmony";
}