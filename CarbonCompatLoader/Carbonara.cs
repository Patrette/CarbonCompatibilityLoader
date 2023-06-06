using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using Carbon.Core;

namespace CarbonCompatLoader;

public static class Carbonara
{
    public static void Run()
    {
        try
        {
            string carbonCore = Path.Combine(Defines.GetManagedFolder(),"Carbon.dll");
            if (!File.Exists(carbonCore)) return;
            ModuleDefinition asm = ModuleDefinition.FromFile(carbonCore);
            TypeDefinition initType = asm.TopLevelTypes.First(x => x.Name == "Initializer" && x.Namespace == "Carbon.Core" && x.Interfaces.Any(y=>y.Interface.Name == "ICarbonComponent"));
            foreach (MethodDefinition method in initType.Methods)
            {
                CilMethodBody body = method.CilMethodBody;
                if (body == null) continue;
                for (int index = 0; index < body.Instructions.Count; index++)
                {
                    CilInstruction CIL = body.Instructions[index];
                    if (CIL.OpCode != CilOpCodes.Ldstr) continue;
                    foreach (KeyValuePair<string,string> entry in StringOverrides)
                    {
                        if (CIL.Operand is string op)
                        {
                            CIL.Operand = op.Replace(entry.Key, entry.Value);
                        }
                    }
                }
            }
            asm.Write(carbonCore);
        }
        catch
        {
            // ignored
        }
    }

    public static Dictionary<string, string> StringOverrides = new Dictionary<string, string>()
    {
        {"CARBON", "CARBONARA"},
        // thx kulltero
        {@"  ______ _______ ______ ______ _______ _______ ", @"  ______ _______ ______ ______ _______ _______ _______ ______ _______ "}, // line 1
        {@" |      |   _   |   __ \   __ \       |    |  |", @" |      |   _   |   __ \   __ \       |    |  |   _   |   __ \   _   |"}, // line 1
        {@" |   ---|       |      <   __ <   -   |       |", @" |   ---|       |      <   __ <   -   |       |       |      <       |"}, // line 1
        {@" |______|___|___|___|__|______/_______|__|____|", @" |______|___|___|___|__|______/_______|__|____|___|___|___|__|___|___|"}, // line 1
    };
    public static bool CanRun()
    {
    #if DEBUG
        return true;
    #endif
        DateTime time = DateTime.Today;
        return time is { Month: 4, Day: 1 };
    }
}