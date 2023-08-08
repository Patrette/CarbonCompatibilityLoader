using System.Reflection;
using System.Runtime.CompilerServices;
using Carbon.Core;
using JetBrains.Annotations;
using Oxide.Core.Extensions;
using Oxide.Core.Plugins;
using Oxide.Plugins;
using Timer = Oxide.Plugins.Timer;

namespace CarbonCompatLoader.Lib;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static partial class OxideCompat
{
    internal static Dictionary<Assembly, ModLoader.ModPackage> modPackages = new();
    public static void RegisterPluginLoader(ExtensionManager self, PluginLoader loader, Extension oxideExt)
    {
        self.RegisterPluginLoader(loader);
        string asmName = Assembly.GetCallingAssembly().GetName().Name;
        Logger.Info($"Oxide plugin loader call using {loader.GetType().FullName} from assembly {asmName}");
        Assembly asm = oxideExt != null ? oxideExt.GetType().Assembly : loader.GetType().Assembly;
        string name = oxideExt != null ? oxideExt.Name : asm.GetName().Name;
        string author = oxideExt != null ? oxideExt.Author : "CCL";
        if (!modPackages.TryGetValue(asm, out ModLoader.ModPackage package))
        {
            package = new ModLoader.ModPackage
            {
                Name = $"{name} - {author} (CCL Oxide Extension)"
            };
            ModLoader.LoadedPackages.Add(package);
            modPackages[asm] = package;
        }
        foreach (Type type in loader.CorePlugins)
        {
            if (type.IsAbstract) continue;
            try
            {
                ModLoader.InitializePlugin(type, out RustPlugin plugin, package, precompiled:true, preInit: oxideExt == null ? null : 
                    rustPlugin =>
                    {
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