using System;
using System.Collections.Generic;
using System.Linq;
using API.Events;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Collections;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using HarmonyLib;

namespace CarbonCompatLoader.Patches;

public static class CodeGenHelpers
{
    public static void GenerateEntrypoint(ModuleDefinition asm, ReferenceImporter importer, string name, Guid guid, out MethodDefinition load, out TypeDefinition typeDef)
    {
        // define type
        TypeDefinition entrypoint = new TypeDefinition($"<__CarbonCompatibilityLoader:{name}__>", $"<entrypoint:{guid:N}>", 
            TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout | TypeAttributes.NotPublic, asm.CorLibTypeFactory.Object.Type
        );
        entrypoint.Interfaces.Add(new InterfaceImplementation(importer.ImportType(typeof(ICarbonCompatExt))));
        entrypoint.AddDefaultCtor(asm, importer);
        
        // define on loaded virtual
        MethodDefinition onLoaded = new MethodDefinition("OnLoaded",
            MethodAttributes.Family |
            MethodAttributes.Final |
            MethodAttributes.HideBySig |
            MethodAttributes.NewSlot |
            MethodAttributes.Virtual,
            MethodSignature.CreateInstance(asm.CorLibTypeFactory.Void));
        entrypoint.Methods.Add(onLoaded);
        asm.TopLevelTypes.Add(entrypoint);
        load = onLoaded;
        typeDef = entrypoint;
    }
    
    public static void GenerateCarbonEventCall(CilMethodBody body, ReferenceImporter importer, ref int index, CarbonEvent eventId, MethodDefinition method, CilInstruction self = null, string event_method = "Subscribe")
    {
        self ??= new CilInstruction(CilOpCodes.Ldnull);
        List<CilInstruction> IL = new List<CilInstruction>()
        {
            new CilInstruction(CilOpCodes.Call, importer.ImportMethod(AccessTools.PropertyGetter(typeof(Carbon.Community), "Runtime"))),
            new CilInstruction(CilOpCodes.Callvirt, importer.ImportMethod(AccessTools.PropertyGetter(typeof(Carbon.Community), "Events"))),
            new CilInstruction(CilOpCodes.Ldc_I4_S, (sbyte)eventId),
            self,
            new CilInstruction(CilOpCodes.Ldftn, method),
            new CilInstruction(CilOpCodes.Newobj, importer.ImportMethod(AccessTools.Constructor(typeof(Action<EventArgs>), new Type[] { typeof(object), typeof(IntPtr) }))),
            new CilInstruction(CilOpCodes.Callvirt, importer.ImportMethod(AccessTools.Method(typeof(API.Events.IEventManager), event_method)))
        };
        body.Instructions.InsertRange(index, IL);
        index += IL.Count;
    }

    public static void DoMultiMethodCall(CilMethodBody body, ref int index, List<MethodDefinition> staticMethods, List<KeyValuePair<TypeDefinition, List<MethodDefinition>>> internalInstances)
    {
        List<CilInstruction> IL = new List<CilInstruction>();
        if (staticMethods != null)
            foreach (MethodDefinition method in staticMethods)
            {
                foreach (Parameter arg in method.Parameters)
                {
                    if (!arg.ParameterType.IsValueType)
                    {
                        IL.Add(new CilInstruction(CilOpCodes.Ldnull));
                        continue;
                    }
    
                    Logger.Error($"Non value type: {arg.ParameterType.ElementType.ToString()}");
                }
                IL.Add(new CilInstruction(CilOpCodes.Call, method));
                if (method.Signature.ReturnsValue)
                {
                    IL.Add(new CilInstruction(CilOpCodes.Pop));
                }
            }

        if (internalInstances != null)
        {
            foreach (KeyValuePair<TypeDefinition, List<MethodDefinition>> instance in internalInstances)
            {
                TypeDefinition type = instance.Key;
                List<MethodDefinition> calls = instance.Value;
                IL.Add(new CilInstruction(CilOpCodes.Newobj, type.Methods.First(x=>x.Parameters.Count == 0 && x.Name == ".ctor")));
                if (calls.Count > 1)
                {
                    for (int i = 0; i < calls.Count-1; i++) // probably a bad idea but who cares
                    {
                        IL.Add(new CilInstruction(CilOpCodes.Dup));
                    }
                }

                foreach (MethodDefinition method in calls)
                {
                    foreach (Parameter arg in method.Parameters)
                    {
                        if (!arg.ParameterType.IsValueType)
                        {
                            IL.Add(new CilInstruction(CilOpCodes.Ldnull));
                            continue;
                        }
    
                        Logger.Error($"Non value type: {arg.ParameterType.ElementType.ToString()}??");
                    }
                    IL.Add(new CilInstruction(CilOpCodes.Callvirt, method));
                    if (method.Signature.ReturnsValue)
                    {
                        IL.Add(new CilInstruction(CilOpCodes.Pop));
                    }
                }
            }
        }
        body.Instructions.InsertRange(index, IL);
        index += IL.Count;
    }
}