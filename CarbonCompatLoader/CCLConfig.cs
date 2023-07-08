using JetBrains.Annotations;

namespace CarbonCompatLoader;

[UsedImplicitly]
public class CCLConfig
{
    public static CCLConfig self;
    // disabled for now
    /*public PluginConverterCFG PluginConverter = new PluginConverterCFG();

    [UsedImplicitly]
    public class PluginConverterCFG
    {
        public bool Enabled = false;
        public List<string> RequiredStrings = new(){"using Harmony;\n"};
        public List<string> PluginWhitelist = new();
    }*/
}