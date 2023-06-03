using System.Text;
using AsmResolver;
using AsmResolver.DotNet;

namespace CarbonCompatLoader;

public static class Helpers
{
    public static bool StartsWith(this Utf8String str, string value) => str.Value.StartsWith(value);
    public static bool EndsWith(this Utf8String str, string value) => str.Value.EndsWith(value);
    public static Utf8String ToLower(this Utf8String str) => new Utf8String(str.Value.ToLower()); // ITypeDescriptor
    
    public static AssemblyReference DefinitionAssembly(this ITypeDescriptor type)
    {
        AssemblyReference asmRef = null;
        while (type != null)
        {
            //Logger.Info($"Type is {type.FullName}");
            type = rec(type, out asmRef);
        }
        //Logger.Info($"Null: {asmRef == null}");
        return asmRef;
        
        ITypeDescriptor rec(ITypeDescriptor type, out AssemblyReference output)
        {
            IResolutionScope rs = type.Scope;
            if (rs is AssemblyReference aref)
            {
                output = aref;
                return null;
            }

            if (rs is ModuleDefinition mdef)
            {
                output = new AssemblyReference(mdef.Assembly);
                return null;
            }

            output = null;
            return type.DeclaringType;
        }
    }
}