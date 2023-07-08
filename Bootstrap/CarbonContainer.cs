using System.Reflection;
using API.Assembly;
using API.Events;
using Carbon.Core;
using JetBrains.Annotations;

namespace CarbonCompatLoader.Bootstrap;

public static class CarbonContainer
{
    public static void Load(byte[] core_raw, List<byte[]> deps_raw, string core_version)
    {
        Bootstrap.logger.Info("Loading dependencies");
        foreach (byte[] dep in deps_raw)
        {
            AssemblyName asm_name = Assembly.Load(dep).GetName();
            Bootstrap.logger.Info($"Loaded: {asm_name.Name}, {asm_name.Version}");
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
            type.GetField("SelfASMRaw", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, core_raw);
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