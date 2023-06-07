using AsmResolver.DotNet;
using CarbonCompatLoader.Converters;
using Oxide.Plugins;

namespace CarbonCompatLoader.Patches.Oxide;

public class OxideTypeRef : BaseOxidePatch
{
    public override void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info)
    {
        foreach (TypeReference tw in asm.GetImportedTypeReferences())
        {
            if (tw.Scope is AssemblyReference aref && aref.Name.StartsWith("Oxide.") && !aref.Name.ToLower().StartsWith("oxide.ext."))
            {
                if (tw.Namespace.StartsWith("Newtonsoft.Json"))
                {
                    tw.Scope = MainConverter.Newtonsoft.ImportWith(importer);
                    continue;
                }
                if (tw.Name == "VersionNumber")
                {
                    goto sdk;
                }
                if (tw.Namespace == "Oxide.Plugins" && tw.Name.EndsWith("Attribute"))
                {
                    tw.Namespace = "";
                    goto sdk;
                }
                if (tw.FullName == "Oxide.Plugins.Hash`2")
                {
                    tw.Namespace = "";
                    goto common;
                }
                if (tw.FullName is "Oxide.Plugins.CSharpPlugin" or "Oxide.Core.Plugins.CSPlugin")
                {
                    tw.Name = "RustPlugin";
                    tw.Namespace = "Oxide.Plugins";
                    goto common;
                }
                if (tw.FullName == "Oxide.Core.Plugins.PluginManager")
                {
                    tw.Namespace = "";
                }
                common:
                tw.Scope = MainConverter.Common.ImportWith(importer);
                continue;
                sdk:
                tw.Scope = MainConverter.SDK.ImportWith(importer);
            }
        }
    }
}