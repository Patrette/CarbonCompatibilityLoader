using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using Carbon.Base;
using CarbonCompatLoader.Converters;

namespace CarbonCompatLoader.Patches.Oxide;

public class OxideTypeRef : BaseOxidePatch
{
    public static List<string> PluginToBaseHookable = new List<string>()
    {
        "System.Void Oxide.Core.Libraries.Permission::RegisterPermission(System.String, Oxide.Core.Plugins.Plugin)"
    };

    public static bool IsOxideASM(AssemblyReference aref)
    {
        return aref.Name.StartsWith("Oxide.") && !aref.Name.ToLower().StartsWith("oxide.ext.");
    }

    public override void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info)
    {
        foreach (MemberReference mref in asm.GetImportedMemberReferences())
        {
            if (!IsOxideASM(mref.DeclaringType.DefinitionAssembly())) continue;
            if (mref.Signature is MethodSignature methodSig)
            {
                if (PluginToBaseHookable.Contains(mref.FullName))
                {
                    for (int index = 0; index < methodSig.ParameterTypes.Count; index++)
                    {
                        TypeSignature typeSig = methodSig.ParameterTypes[index];
                        if (typeSig.FullName == "Oxide.Core.Plugins.Plugin" && IsOxideASM(typeSig.DefinitionAssembly()))
                        {
                            methodSig.ParameterTypes[index] = importer.ImportTypeSignature(typeof(BaseHookable));
                        }
                    }
                }
            }
        }

        foreach (TypeReference tw in asm.GetImportedTypeReferences())
        {
            ProcessTypeRef(tw, importer);
        }

        foreach (TypeDefinition td in asm.GetAllTypes())
        {
            foreach (CustomAttribute attr in td.CustomAttributes)
            {
                for (int index = 0; index < attr.Signature?.FixedArguments.Count; index++)
                {
                    CustomAttributeArgument arg = attr.Signature.FixedArguments[index];
                    if (arg.Element is TypeDefOrRefSignature sig)
                    {
                        ProcessTypeRef(sig.Type as TypeReference, importer);
                    }
                }
            }
        }
    }

    public static void ProcessTypeRef(TypeReference tw, ReferenceImporter importer)
    {
        if (tw == null) return;
        if (tw.Scope is TypeReference parent)
        {
            if (parent.FullName == "Oxide.Plugins.Timer" && tw.Name == "TimerInstance")
            {
                tw.Name = "Timer";
                tw.Namespace = "Oxide.Plugins";
                tw.Scope = MainConverter.Common.ImportWith(importer);
                return;
            }
        }

        if (tw.Scope is AssemblyReference aref && IsOxideASM(aref))
        {
            if (tw.Namespace.StartsWith("Newtonsoft.Json"))
            {
                tw.Scope = MainConverter.Newtonsoft.ImportWith(importer);
                return;
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

            if (tw.FullName is "Oxide.Core.Libraries.Timer")
            {
                tw.Name = "Timer";
                tw.Namespace = "Oxide.Plugins";
                goto common;
            }

            if (tw.FullName == "Oxide.Core.Plugins.HookMethodAttribute")
            {
                tw.Namespace = "";
                goto sdk;
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
            return;
            sdk:
            tw.Scope = MainConverter.SDK.ImportWith(importer);
        }
    }
}