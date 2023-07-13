using System.Reflection;
using API.Assembly;
using API.Events;
using Carbon.Core;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;

namespace CarbonCompatLoader.Bootstrap;

public static class CarbonContainer
{
    public static void LoadConfig(out JObject cfg, out bool canUpdate, out bool enabled)
    {
        canUpdate = true;
        enabled = true;
        cfg = null;
        string cfgPath = Path.Combine(Defines.GetModulesFolder(), "CCL", "config.json");
        if (!File.Exists(cfgPath)) return;
        
        try
        {
            cfg = JObject.Parse(File.ReadAllText(cfgPath));
            JToken at = cfg["Config"]?["bootstrap"]?["AutoUpdates"];
            JToken eb = cfg["Enabled"];
            if (at != null)
            {
                canUpdate = at.ToObject<bool>();
            }
            if (eb != null)
            {
                enabled = eb.ToObject<bool>();
            }
        }
        catch (Exception e)
        {
            Bootstrap.logger.Error($"Failed to load config from {cfgPath}: {e}");
        }
    }
    public static void Load(byte[] core_raw, List<byte[]> deps_raw, string core_version, JObject cfg = null, bool enabled = true)
    {
        if (enabled)
        {
            Bootstrap.logger.Info("Loading dependencies");
            foreach (byte[] dep in deps_raw)
            {
                AssemblyName asm_name = Assembly.Load(dep).GetName();
                Bootstrap.logger.Info($"Loaded: {asm_name.Name}, {asm_name.Version}");
            }
        }

        Bootstrap.logger.Info($"Loading core version {core_version}");
        
        Assembly core = Assembly.Load(core_raw);
        
        Type[] types;
        try
        {
            types = core.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            types = e.Types;
        }
        
        foreach (Type type in types)
        {
            if (type.IsAbstract || !typeof(ICarbonExtension).IsAssignableFrom(type)) continue;
            ICarbonExtension ext = (ICarbonExtension)Activator.CreateInstance(type);
            //type.GetField("SelfASMRaw", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, core_raw);
            type.GetField("bootstrapUsed", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, true);
            if (cfg != null)
            {
                type.GetField("cfg", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, cfg);
            }
            ext.Awake(EventArgs.Empty);
            ext.OnLoaded(EventArgs.Empty);
        }
    }
    [UsedImplicitly]
    class CarbonEntrypoint : ICarbonExtension
    {
        public static bool loaded = false;
        private class UnityLogger : ILogger
        {
            private const string prefix = "[CCL.Bootstrap] ";
            public void Info(object obj)
            {
                UnityEngine.Debug.Log(prefix+obj);
            }

            public void Warn(object obj)
            {
                UnityEngine.Debug.LogWarning(prefix+obj);
            }

            public void Error(object obj)
            {
                UnityEngine.Debug.LogError(prefix+obj);
            }
        }
        
        void ICarbonAddon.Awake(EventArgs args)
        {
            if (loaded) return;
            loaded = true;
            Bootstrap.logger = new UnityLogger();
            string path = Path.Combine(Defines.GetExtensionsFolder(), (string)((CarbonEventArgs)args).Payload);
            Bootstrap.Run(path, path, out byte[] _, load: true);
        }

        void ICarbonAddon.OnLoaded(EventArgs args)
        {

        }

        void ICarbonAddon.OnUnloaded(EventArgs args)
        {

        }
    }
}