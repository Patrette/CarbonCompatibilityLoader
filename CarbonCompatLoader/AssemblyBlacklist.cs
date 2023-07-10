namespace CarbonCompatLoader;

public static class AssemblyBlacklist
{
    public static bool IsInvalid(AssemblyDefinition asm)
    {
        return blacklist.Contains(asm.Name);
    }

    public static List<string> blacklist = new List<string>()
    {
        // Oxide
        
        "Oxide.Core",
        "Oxide.CSharp",
        "Oxide.MySql",
        "Oxide.Unity",
        "Oxide.References",
        "Oxide.Rust",
        "Oxide.SQLite",
        "Oxide.Unity",
        
        // Lib
        
        "0Harmony",

        // ACS
        
        "Assembly-CSharp",
        "Assembly-CSharp-firstpass",
        
        // Facepunch
        
        "Facepunch.BurstCloth",
        "Facepunch.Console",
        "Facepunch.Flexbox",
        "Facepunch.GoogleSheets",
        "Facepunch.Input",
        "Facepunch.Network",
        "Facepunch.Raknet",
        "Facepunch.Rcon",
        "Facepunch.Skeleton",
        "Facepunch.Sqlite",
        "Facepunch.SteamNetworking",
        "Facepunch.Steamworks.Win64",
        "Facepunch.System",
        "Facepunch.Unity",
        "Facepunch.UnityEngine",
        "Facepunch.UnwrapBaker.Settings"
    };
}