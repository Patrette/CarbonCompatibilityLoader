using System.Diagnostics;
using API.Assembly;
using API.Events;
using Carbon;
using Carbon.Core;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Debug = UnityEngine.Debug;

namespace CarbonCompatLoader;

[UsedImplicitly]
internal class CCLEntrypoint : ICarbonExtension
{
    private static void LoadConfig(out bool autoUpdates, out bool eb)
    {
        autoUpdates = true;
        eb = true;
        string cfgPath = Path.Combine(Defines.GetModulesFolder(), "CCL", "config.json");
        try
        {
            if (cfg == null)
            {
                if (!File.Exists(cfgPath)) return;
                cfg = JObject.Parse(File.ReadAllText(cfgPath));
            }

            JToken tk = cfg?["Enabled"];
            
            JToken at = cfg?["Config"]?["bootstrap"]?["AutoUpdates"];

            if (at != null)
            {
                autoUpdates = at.ToObject<bool>();
            }

            if (tk != null)
            {
                eb = tk.ToObject<bool>();
            }
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to load config from {cfgPath}: {e}");
        }
        
    }
    
    //[UsedImplicitly]
    //internal static byte[] SelfASMRaw = null;
    [UsedImplicitly]
    internal static JObject cfg;
    [UsedImplicitly]
    internal static bool bootstrapUsed;
    internal static readonly Version CCLVersion = typeof(MainConverter).Assembly.GetName().Version;
    internal static bool enabled;
    void ICarbonAddon.Awake(EventArgs args)
    {
        LoadConfig(out bool autoUpdates, out enabled);
        Community.Runtime.Events.Subscribe(CarbonEvent.HooksInstalled, _ => CCLInterface.AttemptModuleInit());
        Logger.Info($"Initializing CCL-{MainConverter.BuildConfiguration}-{CCLVersion.ToString(3)}");
        if (!enabled)
        {
            Logger.Warn("CLL is disabled");
            return;
        }
        if (!bootstrapUsed && autoUpdates) Logger.Warn("The bootstrap version is required for auto updates");
        string name = (string)(args is CarbonEventArgs { Payload: string } cargs ? cargs.Payload : null);
        try
        {
            Stopwatch sw = Stopwatch.StartNew();
            MainConverter.Initialize(name);
            Logger.Info("Loading mods");
            MainConverter.LoadAll();
            sw.Stop();
            Logger.Info($"Startup completed in {sw.Elapsed.TotalMilliseconds:n0}ms");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return;
        }
    }

    void ICarbonAddon.OnLoaded(EventArgs args)
    {
        
    }

    void ICarbonAddon.OnUnloaded(EventArgs args)
    {
        
    }
}