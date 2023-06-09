using System.Collections.Generic;
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
        
        // member ref
        
        //new OxideMemberRef(),
        
        // il switch
        new OxideILSwitch(),
        new HarmonyILSwitch(),
        
        // harmony
        new HarmonyBlacklist(),

        // entrypoint
        new OxideEntrypoint(),
        
        // plugins
        new OxidePluginAttr(),
        
        //common
        new AssemblyVersionPatch()
    };
    public override string Path => "oxide";
    public override bool PluginReference => true;
}