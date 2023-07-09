using Carbon.Core;
using Facepunch;

namespace CarbonCompatLoader;

public static class Carbonara
{
    public static void Run()
    {
        try
        {
            //Stopwatch sw = Stopwatch.StartNew();
            string carbonCore = Path.Combine(Defines.GetManagedFolder(),"Carbon.dll");
            if (!File.Exists(carbonCore)) return;
            ModuleDefinition asm = ModuleDefinition.FromFile(carbonCore);
            TypeDefinition initType = asm.TopLevelTypes.First(x => x.Name == "Initializer" && x.Namespace == "Carbon.Core" && x.Interfaces.Any(y=>y.Interface.Name == "ICarbonComponent"));
            bool updated = false;
            foreach (MethodDefinition method in initType.Methods)
            {
                CilMethodBody body = method.CilMethodBody;
                if (body == null) continue;
                for (int index = 0; index < body.Instructions.Count; index++)
                {
                    CilInstruction CIL = body.Instructions[index];
                    if (CIL.OpCode != CilOpCodes.Ldstr) continue;
                    if (CIL.Operand is not string op) continue;
                    foreach (KeyValuePair<string,string> entry in StringReplace)
                    {
                        string modified = op.Replace(entry.Key, entry.Value);
                        if (op == modified) continue;
                        //Logger.Info($"R-Old: {op} R-New: {modified}");
                        CIL.Operand = modified;
                        updated = true;
                    }

                    if (StringOverrides.TryGetValue(op, out string str))
                    {
                        //Logger.Info($"O-Old: {op} O-New: {str}");
                        CIL.Operand = str;
                        updated = true;
                    }
                }
            }
            
            //Logger.Info($"Updated: {updated} in {sw.Elapsed.TotalMilliseconds}ms");
            if (updated)
            {
            #if DEBUG
                Logger.Warn("Patched carbonara");
            #endif
                asm.Write(carbonCore);
            }
            //sw.Start();
            //Logger.Info($"Total time: {sw.Elapsed.TotalMilliseconds:n0}ms");
        }
        catch
        {
            // ignored
        }
    }

    public static readonly Dictionary<string, string> StringOverrides = new Dictionary<string, string>()
    {
        // thx kulltero
        {@"  ______ _______ ______ ______ _______ _______ ", @"  ______ _______ ______ ______ _______ _______ _______ ______ _______ "}, // line 1
        {@" |      |   _   |   __ \   __ \       |    |  |", @" |      |   _   |   __ \   __ \       |    |  |   _   |   __ \   _   |"}, // line 2
        {@" |   ---|       |      <   __ <   -   |       |", @" |   ---|       |      <   __ <   -   |       |       |      <       |"}, // line 3
        {@" |______|___|___|___|__|______/_______|__|____|", @" |______|___|___|___|__|______/_______|__|____|___|___|___|__|___|___|"}, // line 4
    };

    public static readonly Dictionary<string, string> StringReplace = new Dictionary<string, string>()
    {
        { " CARBON ", " CARBONARA " }
    };
    public static bool CanRun()
    {
        if (CommandLine.HasSwitch("-nocarbonara")) return false;
    #if DEBUG
        return true;
    #else
        return DateTime.Today is { Month: 4, Day: 1 };
    #endif
    }
}