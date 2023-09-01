using API.Events;
using Carbon;
using Oxide.Core.Plugins;
using Oxide.Plugins;

namespace CarbonCompatLoader.Lib;

public partial class OxideCompat
{
    static OxideCompat()
    {
        Community.Runtime.Events.Subscribe(CarbonEvent.PluginLoaded, args =>
        {
            HandlePluginIO(true, (CarbonEventArgs)args);
        });
        Community.Runtime.Events.Subscribe(CarbonEvent.PluginUnloaded, args =>
        {
            HandlePluginIO(false, (CarbonEventArgs)args);
        });
    }

    internal static void HandlePluginIO(bool loaded, CarbonEventArgs args)
    {
        RustPlugin plugin = (RustPlugin)args.Payload;
        PluginManagerEvent ev;
        if (loaded)
        {
            ev = plugin.OnAddedToManager as PluginManagerEvent;
        }
        else
        {
            ev = plugin.OnRemovedFromManager as PluginManagerEvent;
        }

        if (ev == null) return;
    #if DEBUG
        Logger.Info($"Calling {(loaded ? "loaded" : "unloaded")} event for plugin {plugin.Name}");
    #endif
        ev.Invoke(plugin, plugin.Manager);
    }
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