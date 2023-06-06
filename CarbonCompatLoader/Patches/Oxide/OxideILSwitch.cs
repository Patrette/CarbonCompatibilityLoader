using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using CarbonCompatLoader.Converters;

namespace CarbonCompatLoader.Patches.Oxide;

public class OxideILSwitch : BaseOxidePatch
{
    public override void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info)
    {
        foreach (TypeDefinition td in asm.GetAllTypes())
        {
            foreach (MethodDefinition method in td.Methods)
            {
                if (!(method.MethodBody is CilMethodBody body)) continue;
                for (int index = 0; index < body.Instructions.Count; index++)
                {
                    CilInstruction CIL = body.Instructions[index];
                    // IL Patches
                    
                }
            }
        }
    }
}