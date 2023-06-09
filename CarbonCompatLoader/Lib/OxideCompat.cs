using System;
using System.Diagnostics;
using System.IO;
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
    public static void AddConsoleCommand1(Oxide.Game.Rust.Libraries.Command Lib, string name, Oxide.Core.Plugins.Plugin plugin, System.Func<ConsoleSystem.Arg, bool> callback)
    {
        Lib.AddConsoleCommand(name, plugin, callback);
    }
    
    public static void AddChatCommand1(Oxide.Game.Rust.Libraries.Command Lib, string name, Oxide.Core.Plugins.Plugin plugin, Action<BasePlayer, string, string[]> callback)
    {
        Lib.AddChatCommand(name, plugin, callback);
    }

    public static string GetExtensionDirectory(Oxide.Core.OxideMod _)
    {
        return Path.Combine(MainConverter.RootDir, MainConverter.Converters["oxide"].Path);
    }
}