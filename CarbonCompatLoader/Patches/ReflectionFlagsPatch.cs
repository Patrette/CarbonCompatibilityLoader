using System.Reflection;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using CarbonCompatLoader.Converters;

namespace CarbonCompatLoader.Patches;

public class ReflectionFlagsPatch : IASMPatch
{
    public static List<string> ReflectionTypeMethods = new List<string>()
    {
        "GetMethod",
        "GetField",
        "GetProperty",
        "GetMember"
    };

    public void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info)
    {
        foreach (TypeDefinition td in asm.GetAllTypes())
        {
            foreach (MethodDefinition method in td.Methods)
            {
                if (method.MethodBody is not CilMethodBody body) continue;
                for (int index = 0; index < body.Instructions.Count; index++)
                {
                    CilInstruction CIL = body.Instructions[index];
                    if (CIL.OpCode == CilOpCodes.Callvirt &&
                        CIL.Operand is MemberReference mref &&
                        mref.Signature is MethodSignature msig &&
                        mref.DeclaringType is TypeReference tref &&
                        tref.Scope is AssemblyReference aref &&
                        aref.IsCorLib &&
                        tref.Name == "Type" &&
                        ReflectionTypeMethods.Contains(mref.Name) &&
                        msig.ParameterTypes.Any(x=>x.Scope is AssemblyReference { IsCorLib: true } && x.Name == "BindingFlags")
                       )
                    {
                        //Logger.Info($"Found binding flags call: {mref.FullName} in method {method.FullName} at {CIL.Offset:x8}");
                        for (int li = index - 1; li >= Math.Max(index-5, 0); li--)
                        {
                            CilInstruction XIL = body.Instructions[li];
                            if (!XIL.IsLdcI4())
                            {
                                //Logger.Info($"{XIL.OpCode.ToString()} is not ldci4");
                                continue;
                            }
                            //Logger.Info($"old: {old}");
                            BindingFlags flags = (BindingFlags)XIL.GetLdcI4Constant() | BindingFlags.Public | BindingFlags.NonPublic;
                            //Logger.Info($"new: {flags}");
                            XIL.Operand = (object)(int)flags;
                            XIL.OpCode = CilOpCodes.Ldc_I4;
                            //Logger.Info($"Changed flags at {XIL.Offset:x8}");
                            goto exit;
                        }
                        Logger.Error("Failed to find binding flags?!");
                    }
                    exit: ;
                }
            }
        }
    }
}