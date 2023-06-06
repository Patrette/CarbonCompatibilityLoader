﻿using System.Collections.Generic;
using CarbonCompatLoader.Patches;
using CarbonCompatLoader.Patches.Harmony;
using CarbonCompatLoader.Patches.Oxide;
using JetBrains.Annotations;

namespace CarbonCompatLoader.Converters;
[UsedImplicitly]
public class HarmonyConverter : BaseConverter
{
    public override List<IASMPatch> patches => new()
    {
        // type ref
        new HarmonyTypeRef(),
        new OxideTypeRef(),
        
        // il switch
        new HarmonyILSwitch(),
        new OxideILSwitch(),
        
        // harmony
        new HarmonyBlacklist(),
        
        // entrypoint
        new HarmonyEntrypoint(),
        
        //common
        new AssemblyVersionPatch()
    };
    public override string Path => "harmony";
}