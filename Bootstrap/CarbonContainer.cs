using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using API.Assembly;
using Carbon.Core;
using UnityEngine;
using Logger = Carbon.Logger;

namespace CarbonCompatLoader.Bootstrap;

public static class CarbonContainer
{
    public static void Load(byte[] core_raw, List<byte[]> deps_raw, string core_version)
    {
        Bootstrap.logger.Info("Loading dependencies");
        foreach (byte[] dep in deps_raw)
        {
            Assembly asm = Assembly.Load(dep);
            AssemblyName asm_name = asm.GetName();
            Bootstrap.logger.Info($"Loaded: {asm_name.Name}, {asm_name.Version}");
        }
        
        Bootstrap.logger.Info($"Loading core version {core_version} {core_raw == null}");
        
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

        types = types.Where(x => typeof(ICarbonExtension).IsAssignableFrom(x) && !x.IsAbstract).ToArray();
        if (types.Length == 0)
        {
            Bootstrap.logger.Error($"No entrypoint for core?!");
            return;
        }
        foreach (Type type in types)
        {
            ICarbonExtension ext = (ICarbonExtension)Activator.CreateInstance(type);
            ext.GetType().GetField("SelfASMRaw", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, core_raw);
            ext.Awake(EventArgs.Empty);
            ext.OnLoaded(EventArgs.Empty);
        }
    }
    class CarbonEntrypoint : ICarbonExtension
    {
        private class UnityLogger : ILogger
        {
            private const string prefix = "[CCL.Bootstrap] ";
            public void Info(object obj)
            {
                Debug.Log(prefix+obj);
            }

            public void Warn(object obj)
            {
                Debug.LogWarning(prefix+obj);
            }

            public void Error(object obj)
            {
                Debug.LogError(prefix+obj);
            }
        }

        void ICarbonAddon.Awake(EventArgs args)
        {
            Bootstrap.logger = new UnityLogger();
            string path = Path.Combine(Defines.GetExtensionsFolder(),
                typeof(CarbonEntrypoint).Assembly.GetName().Name + ".dll");
            Bootstrap.Run(path, path, load: true);
        }

        void ICarbonAddon.OnLoaded(EventArgs args)
        {

        }

        void ICarbonAddon.OnUnloaded(EventArgs args)
        {

        }
    }
}