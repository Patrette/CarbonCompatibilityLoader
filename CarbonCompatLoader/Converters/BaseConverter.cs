using AsmResolver.DotNet.Builder;
using AsmResolver.PE.DotNet.Builder;
using CarbonCompatLoader.Patches;

namespace CarbonCompatLoader.Converters;

public abstract class BaseConverter
{
    public abstract List<IASMPatch> patches { get;}

    public abstract string Path { get; }
    public virtual bool PluginReference => false;
    public string FullPath = null;
    public class GenInfo
    {
        //public AssemblyReference selfRef;

        public bool noEntryPoint = false;

        public string author = null;

        public TokenMapping mappings;

        public GenInfo()//;AssemblyReference self)
        {
            //selfRef = self;
        }
    }
    private static ManagedPEImageBuilder builder = new ManagedPEImageBuilder();
    private static ManagedPEFileBuilder file_builder = new ManagedPEFileBuilder();
    public byte[] Convert(ModuleDefinition asm, out GenInfo info)
    {
        ReferenceImporter importer = new ReferenceImporter(asm);
        info = new GenInfo();//new AssemblyReference(MainConverter.SelfModule.Assembly).ImportWith(importer));
        foreach (IASMPatch patch in patches)
        {
            patch.Apply(asm, importer, info);
        }

        PEImageBuildResult result = builder.CreateImage(asm);

        if (result.HasFailed) throw new MetadataBuilderException("it failed :(");

        info.mappings = (TokenMapping)result.TokenMapping;

        using (MemoryStream ms = new MemoryStream())
        {
            file_builder.CreateFile(result.ConstructedImage).Write(ms);
            return ms.ToArray();
        }
    }
}