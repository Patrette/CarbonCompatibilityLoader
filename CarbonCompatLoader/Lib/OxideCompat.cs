using System.Reflection;
using System.Runtime.CompilerServices;
using Carbon;
using Carbon.Core;
using HarmonyLib;
using JetBrains.Annotations;
using Oxide.Core.Extensions;
using Oxide.Core.Plugins;
using Oxide.Plugins;
using Timer = Oxide.Plugins.Timer;

namespace CarbonCompatLoader.Lib;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static partial class OxideCompat
{
    public static void RegisterPluginLoader(ExtensionManager self, PluginLoader loader, Extension oxideExt)
    {
        self.RegisterPluginLoader(loader);
        string asmName = Assembly.GetCallingAssembly().GetName().Name;
        Logger.Info($"Oxide plugin loader call using {loader.GetType().FullName} from assembly {asmName}");
        foreach (Type type in loader.CorePlugins)
        {
            if (type.IsAbstract) continue;
            //Logger.Info($"  Loading oxide plugin: {type.Name}");
            try
            {
                ModLoader.InitializePlugin(type, out RustPlugin plugin, Community.Runtime.Plugins, precompiled:true, preInit:
                    rustPlugin =>
                    {
                        if (oxideExt == null) return;
                        
                        rustPlugin.Version = oxideExt.Version;
                        if (rustPlugin.Author == "CCL" && !string.IsNullOrWhiteSpace(oxideExt.Author))
                            rustPlugin.Author = oxideExt.Author;
                    });
                plugin.IsExtension = true;
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load plugin {type.Name} in oxide extension {asmName}: {e}");
            }
        }
    }
    
    internal static MethodInfo setIndexAll = AccessTools.Method(typeof(ConsoleSystem.Index), "set_All", new[] {typeof(ConsoleSystem.Command[])});
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetConsoleSystemIndexAll(ConsoleSystem.Command[] value)
    {
        setIndexAll.Invoke(null, new object[] {value} );
    }

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddConsoleCommand1(Oxide.Game.Rust.Libraries.Command Lib, string name, Oxide.Core.Plugins.Plugin plugin, Func<ConsoleSystem.Arg, bool> callback)
    {
        Lib.AddConsoleCommand(name, plugin, callback);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddChatCommand1(Oxide.Game.Rust.Libraries.Command Lib, string name, Oxide.Core.Plugins.Plugin plugin, Action<BasePlayer, string, string[]> callback)
    {
        Lib.AddChatCommand(name, plugin, callback);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T OxideCallHookGeneric<T>(string hook, params object[] args)
    {
        return (T)Oxide.Core.Interface.Call<T>(hook, args);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetExtensionDirectory(Oxide.Core.OxideMod _)
    {
        return Path.Combine(MainConverter.RootDir, MainConverter.Converters["oxide"].Path);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timer TimerOnce(Oxide.Plugins.Timers instance, float delay, Action callback, Plugin owner = null)
    {
        return instance.Once(delay, callback);// ?? throw new NullReferenceException($"Timer-Once is null {instance.Plugin.Name}:{instance.Plugin.GetType().FullName}:{instance.Plugin.GetType().BaseType.FullName} | {instance.IsValid()} {(instance.Plugin == null ? "True" : $"False, {instance.Plugin.IsPrecompiled}:{Community.IsServerFullyInitialized}")} {instance.Persistence == null}");
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timer TimerRepeat(Oxide.Plugins.Timers instance, float delay, int reps, Action callback, Plugin owner = null)
    {
        return instance.Repeat(delay, reps, callback);// ?? throw new NullReferenceException($"Timer-Repeat is null | {instance.IsValid()} {instance.Plugin == null} {instance.Persistence == null}");
    }
}