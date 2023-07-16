using JetBrains.Annotations;

namespace CarbonCompatLoader;

[UsedImplicitly]
public class CCLConfig
{
    public static CCLConfig self;

    public BootstrapConfig bootstrap = new();

    public DevConfig Development = new();
    
    public class BootstrapConfig
    {
        public bool AutoUpdates = 
            #if DEBUG
                false
            #else
                true
            #endif
                ;
    }

    public class DevConfig
    {
        public bool DevMode = false;
        public List<string> ReferenceAssemblies = new();
    }
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