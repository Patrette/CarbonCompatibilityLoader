using System.Collections.Generic;
using AsmResolver.DotNet;
using CarbonCompatLoader.Patches;
using CarbonCompatLoader.Patches.Harmony;
using CarbonCompatLoader.Patches.Oxide;

namespace CarbonCompatLoader.Converters;

public class HarmonyConverter : BaseConverter
{
    public override List<IASMPatch> patches => new()
    {
        // harmony
        new HarmonyTypeRef(),
        new HarmonyILSwitch(),
        new HarmonyBlacklist(),
        
        //oxide
        new OxideTypeRef(),
        
        //common
        new AssemblyVersionPatch()
    };
    public override string Path => "harmony";
}