using System.Reflection;
using Newtonsoft.Json;

namespace CarbonCompatLoader.Bootstrap;

public class AssemblyManifest
{
    public class ASMDependency
    {
        public string id;
        public string version;
        public List<int> installed = new List<int>();

        [JsonIgnore] 
        public List<byte[]> data = null;

        public bool LoadDataFromASM(Assembly asm)
        {
            if (data != null) return true;
            data = new List<byte[]>();
            foreach (int index in installed)
            {
                if (!Bootstrap.TryGetLZ4ResourceStream(string.Format(Bootstrap.dependencyFormatString, index), asm, out Stream stream))
                {
                    return false;
                }
                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    stream.Dispose();
                    data.Add(ms.ToArray());
                }
            }

            return true;
        }
    }

    public bool EnsureResolved(Assembly asm, ref byte[] core)
    {
        foreach (ASMDependency dep in dependencies)
        {
            if (dep.data == null && !dep.LoadDataFromASM(asm)) return false;
        }

        return ResolveCore(asm, ref core);
    }

    public bool ResolveCore(Assembly asm, ref byte[] extData)
    {
        if (extData == null)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                if (Bootstrap.TryGetLZ4ResourceStream(Bootstrap.extensionResourceName, asm, out Stream data))
                {
                    data.CopyTo(ms);
                    data.Dispose();
                    extData = ms.ToArray();
                }
                else
                {
                    return false;
                }
            }
        }
        return true;
    }

    public int protocol = 0;
    
    public bool valid = false;

    public string extensionVersion = "none";

    public List<ASMDependency> dependencies = new List<ASMDependency>();
}

public class DownloadManifest
{
    public class DLDependency
    {
        public AssemblySource type;
        public string id;
        public string version;
        //public string url;

        public AssemblyManifest.ASMDependency ToASM() => new AssemblyManifest.ASMDependency()
        {
            id = id,
            version = version
        };
    }

    public string extensionVersion;

    public string extensionName;
    
    public string bootstrapVersion;

    public string bootstrapName;
    
    public List<DLDependency> dependencies = new List<DLDependency>();
}

public enum AssemblySource
{
    NuGet = 0,
    //DirectURL = 2,
    
}