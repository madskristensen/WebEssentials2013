using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.JSON.Core.Schema;
using Microsoft.Web.Editor;
using Minimatch;
using Newtonsoft.Json.Linq;

namespace MadsKristensen.EditorExtensions.JSON
{
    [Export(typeof(IJSONSchemaSelector))]
    internal class SchemaExporter : IJSONSchemaSelector
    {
        private static string _path = Path.Combine(WebEditor.Host.UserFolder, @"schemas\json\catalog\schemastore.json");
        private static Dictionary<string, IEnumerable<string>> _schemas = new Dictionary<string, IEnumerable<string>>();
        private static Options _options = new Options { AllowWindowsPaths = true };
        private static bool _isDownloading;
        private const int _days = 3;

        public IEnumerable<string> GetAvailableSchemas()
        {
            string catalog = GetCataLog();

            if (!string.IsNullOrEmpty(catalog))
                ParseJsonCatalog(catalog);

            return _schemas.Keys;
        }

        private static string GetCataLog()
        {
            FileInfo file = new FileInfo(_path);

            if (!file.Directory.Exists)
                file.Directory.Create();

            if (!file.Exists)
            {
                Task.Run(() => DownloadCatalog());
                return null;
            }
            else if (file.LastWriteTime < DateTime.Now.AddDays(-_days))
            {
                Task.Run(() => DownloadCatalog());
            }

            return File.ReadAllText(_path);
        }

        private static void DownloadCatalog()
        {
            if (_isDownloading)
                return;

            try
            {
                _isDownloading = true;

                using (var client = new WebClient())
                {
                    string catalog = client.DownloadString("http://schemastore.org/api/json/catalog.json");
                    File.WriteAllText(_path, catalog);
                }
            }
            catch
            {
                Logger.Log("JSON Schema: Couldn't download the catalog file");
            }
            finally
            {
                _isDownloading = false;
            }
        }

        private static void ParseJsonCatalog(string catalog)
        {
            try
            {
                JObject json = JObject.Parse(catalog);
                var schemas = (JArray)json["schemas"];

                foreach (var schema in schemas)
                {
                    try
                    {
                        IEnumerable<string> files;
                        var array = schema["fileMatch"] as JArray;

                        if (array != null)
                            files = array.Children().Select(c => (string)c).ToList();
                        else
                            files = new[] { (string)schema["fileMatch"] };

                        string url = (string)schema["url"];

                        if (!string.IsNullOrEmpty(url) && !_schemas.ContainsKey(url))
                            _schemas.Add(url, files);
                    }
                    catch
                    {
                        Logger.Log("JSON Schema: Couldn't parse the schema catalog");
                    }
                }
            }
            catch
            {
                Logger.Log("JSON Schema: The catalog file was not in the right format");
            }

        }

        public string GetSchemaFor(string fileLocation)
        {
            foreach (string url in _schemas.Keys)
            {
                try
                {
                    if (_schemas[url] == null)
                        continue;
                    foreach (string file in _schemas[url])
                    {
                        var pattern = "**/" + file.TrimStart('*', '/');

                        var matche = new Minimatcher(pattern, _options);
                        if (matche.IsMatch(fileLocation))
                            return url;
                    }
                }
                catch
                {
                    Logger.Log("JSON Schema: Couldn't parse " + _schemas[url] + " as a valid regex.");
                }
            }

            return null;
        }
    }
}