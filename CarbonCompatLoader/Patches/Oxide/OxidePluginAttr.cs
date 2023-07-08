using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using CarbonCompatLoader.Converters;
using Facepunch.Crypt;

namespace CarbonCompatLoader.Patches.Oxide;

public class OxidePluginAttr : BaseOxidePatch
{
    public override void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info)
    {
        string author = info.author ?? "CCL";
        foreach (TypeDefinition td in asm.GetAllTypes())
        {
            if (!td.IsBaseType(x => x.Name == "RustPlugin" && x.DefinitionAssembly().Name == "Carbon.Common")) continue;
            
            {
                if (td.Name.ToString().IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                {
                    string newName = "plugin_" + Md5.Calculate(td.Name);
                    Logger.Warn($"Plugin \"{td.Name}\" has an invalid name, renaming to {newName}");
                    td.Name = newName;
                }
                CustomAttribute infoAttr = td.CustomAttributes.FirstOrDefault(x =>
                    x.Constructor.DeclaringType.FullName == "InfoAttribute" &&
                    x.Constructor.DeclaringType.DefinitionAssembly().Name == "Carbon.Common");
                if (infoAttr != null) continue;
                td.CustomAttributes.Add(
                    new CustomAttribute(
                        importer.ImportType(typeof(InfoAttribute)).CreateMemberReference(".ctor",
                            MethodSignature.CreateInstance(asm.CorLibTypeFactory.Void, asm.CorLibTypeFactory.String,
                                asm.CorLibTypeFactory.String, asm.CorLibTypeFactory.Double)).ImportWith(importer))
                    {
                        Signature = new CustomAttributeSignature(
                            new CustomAttributeArgument(asm.CorLibTypeFactory.String, $"{asm.Assembly.Name}-{td.Name}"),
                            new CustomAttributeArgument(asm.CorLibTypeFactory.String, author),
                            new CustomAttributeArgument(asm.CorLibTypeFactory.Double, 0d))
                    });
            }
        }
    }
}