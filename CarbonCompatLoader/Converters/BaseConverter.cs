using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AsmResolver.DotNet;
using CarbonCompatLoader.Patches;
using JetBrains.Annotations;
using UnityEngine;

namespace CarbonCompatLoader.Converters;

public abstract class BaseConverter
{
    public abstract List<IASMPatch> patches { get;}

    public abstract string Path { get; }

    public string FullPath = null;
    public struct GenInfo
    {
        public AssemblyReference selfRef;

        public GenInfo(AssemblyReference self)
        {
            selfRef = self;
        }
    }
    public byte[] Convert(ModuleDefinition asm)
    {
        ReferenceImporter importer = new ReferenceImporter(asm);
        GenInfo info = new GenInfo(new AssemblyReference(MainConverter.SelfModule.Assembly).ImportWith(importer));
        foreach (IASMPatch patch in patches)
        {
            patch.Apply(asm, importer, info);
        }

        using (MemoryStream ms = new MemoryStream())
        {
            asm.Write(ms);
            return ms.ToArray();
        }
    }
}