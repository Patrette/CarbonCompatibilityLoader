using AsmResolver.DotNet.Signatures.Types;
using Carbon.Base;
using CarbonCompatLoader.Converters;
using CarbonCompatLoader.Lib;

namespace CarbonCompatLoader.Patches.Oxide;

public class OxideTypeRef : BaseOxidePatch
{
    public static List<string> PluginToBaseHookable = new List<string>()
    {
        "System.Void Oxide.Core.Libraries.Permission::RegisterPermission(System.String, Oxide.Core.Plugins.Plugin)",
        "System.Void Oxide.Core.Libraries.Lang::RegisterMessages(System.Collections.Generic.Dictionary`2<System.String, System.String>, Oxide.Core.Plugins.Plugin, System.String)",
        "System.String Oxide.Core.Libraries.Lang::GetMessage(System.String, Oxide.Core.Plugins.Plugin, System.String)",
    };

    public static bool IsOxideASM(AssemblyReference aref)
    {
        return aref.Name.StartsWith("Oxide.") && !aref.Name.ToLower().StartsWith("oxide.ext.");
    }

    public override void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info)
    {
        foreach (MemberReference mref in asm.GetImportedMemberReferences())
        {
            AssemblyReference aref = mref.DeclaringType.DefinitionAssembly();
            if (mref.Signature is MethodSignature methodSig)
            {
                if (IsOxideASM(aref))
                {
                    if (PluginToBaseHookable.Contains(mref.FullName))
                    {
                        for (int index = 0; index < methodSig.ParameterTypes.Count; index++)
                        {
                            TypeSignature typeSig = methodSig.ParameterTypes[index];
                            if (typeSig.FullName == "Oxide.Core.Plugins.Plugin" &&
                                IsOxideASM(typeSig.DefinitionAssembly()))
                            {
                                methodSig.ParameterTypes[index] = importer.ImportTypeSignature(typeof(BaseHookable));
                            }
                        }
                    }
                    /*else
                    {
                        Logger.Info($"call: {mref.FullName}");
                    }*/
                    continue;
                }

                if (aref.Name == "Facepunch.Console" && mref.FullName == "System.Void ConsoleSystem+Index::set_All(ConsoleSystem+Command[])")
                {
                    mref.Name = nameof(OxideCompat.SetConsoleSystemIndexAll);
                    mref.Parent = importer.ImportType(typeof(OxideCompat));
                }
            }
        }

        foreach (TypeReference tw in asm.GetImportedTypeReferences())
        {
            ProcessTypeRef(tw, importer);
        }
        
        ProcessAttrList(asm.CustomAttributes);
        foreach (TypeDefinition td in asm.GetAllTypes())
        {
            ProcessAttrList(td.CustomAttributes);
            
            foreach (FieldDefinition field in td.Fields)
            {
                ProcessAttrList(field.CustomAttributes);
            }

            foreach (MethodDefinition method in td.Methods)
            {
                ProcessAttrList(method.CustomAttributes);
            }

            foreach (PropertyDefinition prop in td.Properties)
            {
                ProcessAttrList(prop.CustomAttributes);
            }
        }

        void ProcessAttrList(IList<CustomAttribute> list)
        {
            for (int x = 0; x < list.Count; x++)
            {
                CustomAttribute attr = list[x];
                for (int y = 0; y < attr.Signature?.FixedArguments.Count; y++)
                {
                    CustomAttributeArgument arg = attr.Signature.FixedArguments[y];
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
            if (parent.FullName is "Oxide.Plugins.Timers" or "Oxide.Plugins.Timer" && tw.Name == "TimerInstance")
            {
                tw.Name = "Timer";
                tw.Namespace = "Oxide.Plugins";
                tw.Scope = MainConverter.Common.ImportWith(importer);
                return;
            }
        }

        if (tw.Scope is AssemblyReference aref && IsOxideASM(aref))
        {
            if (tw.FullName == "Oxide.Core.Event" || tw.FullName.StartsWith("Oxide.Core.Event`"))
            {
                tw.Scope = (IResolutionScope)importer.ImportType(typeof(OxideCompat));
                return;
            }
            
            if (tw.Namespace.StartsWith("Newtonsoft.Json"))
            {
                tw.Scope = MainConverter.Newtonsoft.ImportWith(importer);
                return;
            }
            
            if (tw.Namespace.StartsWith("ProtoBuf"))
            {
                if (tw.Namespace == "ProtoBuf" && tw.Name == "Serializer") 
                    tw.Scope = MainConverter.protobuf.ImportWith(importer);
                else
                    tw.Scope = MainConverter.protobufCore.ImportWith(importer);
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
                tw.Name = "Timers";
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