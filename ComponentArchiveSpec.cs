using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ValheimExtComponentManager
{
    public class ComponentArchiveSpec
    {
        private readonly string baseUrl;
        private readonly Dictionary<string, string> components;

        public ComponentArchiveSpec(string baseUrl)
        {
            this.baseUrl = baseUrl;
            this.components = new Dictionary<string, string>();
        }

        private void ParseFile(string fileContent)
        {
            Console.WriteLine("Pulled content:");
            Console.WriteLine(fileContent);

            // Implementation of file parsing logic
            string[] lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                Console.WriteLine("Processing line: " + line);
                int index = line.IndexOf('=');
                if (index != -1)
                {
                    string key = line.Substring(0, index).Trim();
                    string value = line.Substring(index + 1).Trim();
                    this.components[key] = value;
                }
            }
        }

        public string GetComponentArchive(string componentName)
        {
            if (this.components.TryGetValue(componentName, out string archive))
            {
                if (archive.StartsWith("http://") || archive.StartsWith("https://"))
                {
                    Uri uri = new Uri(archive);
                    string filename = uri.Segments[^1];
                    int paramIndex = filename.IndexOf('?');
                    if (paramIndex != -1)
                    {
                        filename = filename.Substring(0, paramIndex);
                    }
                    return filename;
                }
                else
                {
                    int paramIndex = archive.IndexOf('?');
                    if (paramIndex != -1)
                    {
                        archive = archive.Substring(0, paramIndex);
                    }
                    return archive;
                }
            }
            return null;
        }

        public string GetComponentArchiveUrl(string componentName)
        {
            if (this.components.TryGetValue(componentName, out string archive))
            {
                if (archive.StartsWith("http://") || archive.StartsWith("https://"))
                {
                    return archive;
                }
                else
                {
                    string newUrl = this.baseUrl + archive;
                    return newUrl;
                }
            }
            return null;
        }

        public static async Task<ComponentArchiveSpec> PullSpec()
        {
            string url = "https://www.dropbox.com/scl/fi/qk55f7qsio897bba5pdgu/manager-config.spec?rlkey=oricfpeq1bo9cbnmkygywn0pc&st=bwbj8dbk&dl=1";
            DownloadUtil downloadUtil = new DownloadUtil();
            string content = await downloadUtil.ReadContentAsStringAsync(url);
            Uri baseUri = new Uri(url);
            string baseUrl = baseUri.GetLeftPart(UriPartial.Path).Substring(0, baseUri.GetLeftPart(UriPartial.Path).LastIndexOf('/') + 1);
            ComponentArchiveSpec spec = new ComponentArchiveSpec(baseUrl);
            spec.ParseFile(content);
            return spec;
        }
    }
}