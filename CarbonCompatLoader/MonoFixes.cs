using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;

namespace CarbonCompatLoader;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class MonoFixes
{
    [HarmonyPatch(typeof(AssemblyName), nameof(AssemblyName.CultureName), MethodType.Setter)]
    private static class AssemblyNameCultureFix
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> op, ILGenerator ILGen)
        {
            return new CodeInstruction[]{new CodeInstruction(OpCodes.Ret)};
        }
    }
}