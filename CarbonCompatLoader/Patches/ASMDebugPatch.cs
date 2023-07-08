using System.Diagnostics;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using CarbonCompatLoader.Converters;

namespace CarbonCompatLoader.Patches;

public class ASMDebugPatch : IASMPatch
{
    public void Apply(ModuleDefinition asm, ReferenceImporter importer, BaseConverter.GenInfo info)
    {
        for (int index = 0; index < asm.Assembly.CustomAttributes.Count; index++)
        {
            CustomAttribute attr = asm.Assembly.CustomAttributes[index];
            if (
                attr.Constructor.DeclaringType.FullName == "System.Diagnostics.DebuggableAttribute" &&
                attr.Constructor.DeclaringType.DefinitionAssembly().IsCorLib)
            {
                asm.Assembly.CustomAttributes.RemoveAt(index--);
            }
        }

        TypeSignature enumRef = importer.ImportTypeSignature(typeof(DebuggableAttribute.DebuggingModes));
        CustomAttribute debugAttr = new CustomAttribute(importer.ImportType(typeof(DebuggableAttribute))
                .CreateMemberReference(".ctor",
                    MethodSignature.CreateInstance(asm.CorLibTypeFactory.Void,
                        importer.ImportTypeSignature(typeof(DebuggableAttribute.DebuggingModes)))).ImportWith(importer),
            new CustomAttributeSignature(new CustomAttributeArgument(enumRef,
                (int)(DebuggableAttribute.DebuggingModes.DisableOptimizations |
                      DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints |
                      DebuggableAttribute.DebuggingModes.EnableEditAndContinue))));

        asm.Assembly.CustomAttributes.Add(debugAttr);
    }
}