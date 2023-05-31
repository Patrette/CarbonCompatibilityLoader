using System.Collections.Generic;
using AsmResolver.DotNet;
using CarbonCompatLoader.Patches;
using CarbonCompatLoader.Patches.Harmony;
using CarbonCompatLoader.Patches.Oxide;

namespace CarbonCompatLoader.Converters;

public class OxideConverter : BaseConverter
{
    public override List<IASMPatch> patches => new()
    {
        new OxideTypeRef(),
        new HarmonyTypeRef(),
        
        new AssemblyVersionPatch()
    };
    public override string Path => "oxide";
}