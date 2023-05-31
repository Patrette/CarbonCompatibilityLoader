using System;
using API.Assembly;

namespace CarbonCompatLoader;

public class CCLCore : ICarbonExtension
{
    void ICarbonAddon.Awake(EventArgs args)
    {
        Logger.Info("Initializing");
        MainConverter.Initialize();
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