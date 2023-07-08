using Oxide.Core.Plugins;

namespace CarbonCompatLoader.Lib;

public partial class OxideCompat
{
    public class PluginManagerEvent : Event<Plugin, PluginManager>
    {
        
    }

    public static PluginManagerEvent OnAddedToManagerCompat(Plugin plugin)
    {
        if (plugin.OnAddedToManager is not PluginManagerEvent)
            plugin.OnAddedToManager = new PluginManagerEvent();
        
        PluginManagerEvent ev = (PluginManagerEvent)plugin.OnAddedToManager;
        return ev;
    }
    
    public static PluginManagerEvent OnRemovedFromManagerCompat(Plugin plugin)
    {
        if (plugin.OnRemovedFromManager is not PluginManagerEvent)
            plugin.OnRemovedFromManager = new PluginManagerEvent();
        
        PluginManagerEvent ev = (PluginManagerEvent)plugin.OnRemovedFromManager;
        return ev;
    }
}