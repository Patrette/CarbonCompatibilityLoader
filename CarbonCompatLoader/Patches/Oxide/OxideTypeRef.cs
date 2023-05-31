using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using CarbonCompatLoader.Converters;

namespace CarbonCompatLoader.Patches.Oxide;

public class OxideTypeRef : BaseOxidePatch
{
    public override void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info)
    {
        foreach (TypeReference tw in asm.GetImportedTypeReferences())
        {
            if (tw.Scope is AssemblyReference aref && aref.Name.StartsWith("Oxide.") && !aref.Name.ToLower().StartsWith("oxide.ext."))
            {
                /*if (tw.Name == "CSPlugin")
                {
                    Console.WriteLine("found csss");
                    tw.Name = CommonPlugin.Name;
                    tw.Namespace = CommonPlugin.Namespace;
                }*/
                if (tw.Name == "VersionNumber")
                {
                    tw.Scope = MainConverter.SDK.ImportWith(importer);
                    continue;
                }
                if (tw.Namespace == "Oxide.Plugins" && tw.Name.EndsWith("Attribute"))
                {
                    tw.Scope = MainConverter.SDK.ImportWith(importer);
                    tw.Namespace = "";
                    continue;
                }
                if (tw.Namespace.StartsWith("Newtonsoft.Json"))
                {
                    //Console.WriteLine($"adding newtonsoft {tw.FullName}");
                    tw.Scope = MainConverter.Newtonsoft.ImportWith(importer);
                    continue;
                }
                if (tw.FullName == "Oxide.Plugins.Hash`2")
                {
                    tw.Namespace = "";
                    tw.Scope = MainConverter.Common.ImportWith(importer);
                    continue;
                }
                if (tw.FullName == "Oxide.Plugins.CSharpPlugin")
                {
                    tw.Name = "RustPlugin";
                    tw.Scope = MainConverter.Common.ImportWith(importer);
                    continue;
                }

                if (tw.FullName == "Oxide.Core.Plugins.PluginManager")
                {
                    tw.Namespace = "";
                    tw.Scope = MainConverter.Common.ImportWith(importer);
                    continue;
                }

                tw.Scope = MainConverter.Common.ImportWith(importer);
                //if (tw.Name.StartsWith("CallHook")) Console.WriteLine(tw.FullName);
            }
        }
    }
}