using API.Assembly;
using API.Events;
using UnityEngine;

namespace CarbonCompatLoader;

internal class CCLCore : ICarbonExtension
{
    internal static byte[] SelfASMRaw = null;
    void ICarbonAddon.Awake(EventArgs args)
    {
        Logger.Info($"Initializing CCL-{MainConverter.BuildConfiguration}");
        string name = (string)(args is CarbonEventArgs { Payload: string } cargs ? cargs.Payload : null);
        try
        {
            MainConverter.Initialize(name);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return;
        }
        Logger.Info("Loading mods");
        MainConverter.LoadAll();
    }

    void ICarbonAddon.OnLoaded(EventArgs args)
    {
        
    }

    void ICarbonAddon.OnUnloaded(EventArgs args)
    {
        
    }
}