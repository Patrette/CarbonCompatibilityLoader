using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;

namespace CarbonCompatLoader.Bootstrap;

public static class AssemblyDownloader
{
    public const string GithubDownloadUrl = "https://github.com/Patrette/CarbonCompatibilityLoader/releases/download/{0}/{1}";
    public static HttpClient http = new HttpClient();
    public static string NuGetCache = null;
    public static bool WriteCache = false;
    public static bool DownloadNuGetPackage(string id, string version, out List<byte[]> data)
    {
        string path = $"{id}/{version}/{id}.{version}.nupkg";
        Stream zipStream;
        if (NuGetCache != null)
        {
            string fullPath = Path.Combine(NuGetCache, path);
            Bootstrap.logger.Info($"Trying to find {fullPath}");
            if (File.Exists(fullPath))
            {
                Bootstrap.logger.Info($"Loading {id} ver {version} from cache");
                zipStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                goto start;
            }
        }
        Bootstrap.logger.Info($"Downloading {id} ver {version} using NuGet");
        string url = $"https://api.nuget.org/v3-flatcontainer/{path}";
        HttpResponseMessage rs = http.GetAsync(url).Result;
        if (rs.StatusCode != HttpStatusCode.OK)
        {
            data = null;
            return false;
        }

        zipStream = rs.Content.ReadAsStreamAsync().Result;
        if (WriteCache && NuGetCache != null)
        {
            Bootstrap.logger.Info($"Writing {id} ver {version} to cache");
            string fullPath = Path.Combine(NuGetCache, path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            using (FileStream fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                zipStream.CopyTo(fs);
            }
        }
        start:
        data = new List<byte[]>();
        using (ZipArchive zip = new ZipArchive(zipStream, ZipArchiveMode.Read))
        {
            foreach (ZipArchiveEntry entry in zip.Entries)
            {
                string dir = Path.GetDirectoryName(entry.FullName);
                if (dir == null) continue;
                string name = Path.GetFileName(entry.FullName);
                if (name.EndsWith(".dll") && (dir.StartsWith("lib\\netframework4.8") || dir.StartsWith("lib\\netstandard2.0")))
                {
                    Bootstrap.logger.Info($"Adding {entry.FullName}");
                    using (Stream stream = entry.Open())
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            data.Add(ms.ToArray());
                        }
                    }
                }
            }
        }

        return data.Count > 0;
    }
    public static bool DownloadBytesFromGithubRelease(string branch, string name, out byte[] data)
    {
        string url = string.Format(GithubDownloadUrl, branch, name);
        HttpResponseMessage rs = http.GetAsync(url).Result;
        if (rs.StatusCode != HttpStatusCode.OK)
        {
            data = null;
            return false;
        }

        data = rs.Content.ReadAsByteArrayAsync().Result;
        return true;
    }
    public static bool DownloadStringFromGithubRelease(string branch, string name, out string data)
    {
        string url = string.Format(GithubDownloadUrl, branch, name);
        HttpResponseMessage rs = http.GetAsync(url).Result;
        if (rs.StatusCode != HttpStatusCode.OK)
        {
            data = null;
            return false;
        }

        data = rs.Content.ReadAsStringAsync().Result;
        return true;
    }
}