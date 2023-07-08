using CarbonCompatLoader.Patches;
using CarbonCompatLoader.Patches.Harmony;
using CarbonCompatLoader.Patches.Oxide;
using JetBrains.Annotations;

namespace CarbonCompatLoader.Converters;

[UsedImplicitly]
public class PluginConverter : BaseConverter
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
        new HarmonyBlacklist()
    };

    public static PluginConverter self;
    
    public PluginConverter()
    {
        if (self != null) return;
        if (CCLConfig.self.PluginConverter.Enabled)
        {
            self = this;
            Init();
        }
    }

    public bool CanConvertPlugin(string name, string source)
    {
        if (!CCLConfig.self.PluginConverter.Enabled) return false;
        if (CCLConfig.self.PluginConverter.PluginWhitelist.Count > 0)
        {
            return CCLConfig.self.PluginConverter.PluginWhitelist.Contains(name);
        }
        if (CCLConfig.self.PluginConverter.RequiredStrings.Count > 0)
        {
            return CCLConfig.self.PluginConverter.RequiredStrings.Any(source.Contains);
        }

        return true;
    }

    public void Init()
    {
        
    }
    public override string Path => null;
}