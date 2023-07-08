using API.Assembly;
using UnityEngine;

namespace CarbonCompatLoader;

internal class CCLCore : ICarbonExtension
{
    internal static byte[] SelfASMRaw = null;
    void ICarbonAddon.Awake(EventArgs args)
    {
        Logger.Info("Initializing");
        try
        {
            MainConverter.Initialize();
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