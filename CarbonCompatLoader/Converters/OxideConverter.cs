using System.Collections.Generic;
using AsmResolver.DotNet;
using CarbonCompatLoader.Patches;
using CarbonCompatLoader.Patches.Harmony;
using CarbonCompatLoader.Patches.Oxide;
using JetBrains.Annotations;

namespace CarbonCompatLoader.Converters;
[UsedImplicitly]
public class OxideConverter : BaseConverter
{
    public override List<IASMPatch> patches => new()
    {
        // type ref
        new OxideTypeRef(),
        new HarmonyTypeRef(),
        
        // il switch
        new OxideILSwitch(),
        new HarmonyILSwitch(),
            
        // harmony
        new HarmonyBlacklist(),
        
        //common
        new AssemblyVersionPatch()
    };
    public override string Path => "oxide";
}