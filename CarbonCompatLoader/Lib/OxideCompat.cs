using System;
using System.Diagnostics;
using System.Reflection;
using Carbon;
using Carbon.Core;
using JetBrains.Annotations;
using Oxide.Core.Extensions;
using Oxide.Core.Plugins;
using Oxide.Plugins;

namespace CarbonCompatLoader.Lib;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class OxideCompat
{
    public static void RegisterPluginLoader(ExtensionManager self, PluginLoader loader)
    {
        self.RegisterPluginLoader(loader);
        string asmName = Assembly.GetCallingAssembly().GetName().Name;
        Logger.Info($"Oxide plugin loader call using {loader.GetType().FullName} from assembly {asmName}");
        foreach (Type type in loader.CorePlugins)
        {
            Logger.Info($"  Loading oxide plugin: {type.Name}");
            try
            {
                ModLoader.InitializePlugin(type, out RustPlugin plugin, Community.Runtime.Plugins);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load plugin {type.Name} in oxide extension {asmName}: {e}");
            }
        }
    }
}