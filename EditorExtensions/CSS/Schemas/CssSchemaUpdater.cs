using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.CSS.Editor.Schemas;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions.Css
{
    internal class CssSchemaUpdater
    {
        private static string _path = Path.Combine(WebEditor.Host.UserFolder, @"schemas\css");
        private static DateTime _lastRequest;
        private const int _days = 3;

        public static void CheckForUpdates()
        {
            if (_lastRequest.AddDays(_days) > DateTime.UtcNow)
                return;

            try
            {
                _lastRequest = GetLastRequestDate();

                if (_lastRequest.Year == 2000 || _lastRequest.AddDays(_days) < DateTime.UtcNow)
                {
                    Task.Run(async () =>
                    {
                        await ProcessSchemaUpdate();
                        _lastRequest = DateTime.UtcNow;
                    });
                }
            }
            catch
            {
                // File permissions are not granted
                WriteLog("Error: File permissions are not granted");
            }
        }

        private async static Task ProcessSchemaUpdate()
        {
            XmlDocument doc = DownloadXml();

            if (doc != null)
            {
                string folder = GetFolder();
                int filesWritten = await WriteFilesToDisk(folder, doc);

                if (filesWritten > 0)
                {
                    CssSchemaManager.SchemaManager.ReloadSchemas();
                    WriteLog(filesWritten + " CSS schema files updated");
                }
                else
                {
                    WriteLog("No CSS schema updates found");
                }
            }
        }

        private async static Task<int> WriteFilesToDisk(string folder, XmlDocument doc)
        {
            try
            {
                XmlNodeList nodes = doc.SelectNodes("//CssModule");

                foreach (XmlNode node in nodes)
                {
                    string fileName = node.Attributes["fileName"].InnerText;
                    string path = Path.Combine(folder, fileName);

                    await FileHelpers.WriteAllTextRetry(path, node.OuterXml, false);
                    WriteLog("Updating module: " + fileName);
                }

                return nodes.Count;
            }
            catch
            {
                WriteLog("Error writing new schema files to disk");
                return 0;
            }
        }

        private static XmlDocument DownloadXml()
        {
            string url = "http://realworldvalidator.com/api?date=" + _lastRequest.AddDays(-7).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            XmlDocument doc = new XmlDocument();

            try
            {
                WriteLog("Looking for new CSS schema files...");

                using (WebClient client = new WebClient())
                {
                    doc.LoadXml(client.DownloadString(url));
                }

                WriteLog("New schema files downloaded");
                return doc;
            }
            catch
            {
                WriteLog("Failed to download schema files");
                return null;
            }
        }

        private static void WriteLog(string message)
        {
            string logPath = GetLogFilePath();

            using (StreamWriter writer = File.AppendText(logPath))
            {
                writer.WriteLine(DateTime.Now + " " + message);
            }

            WebEssentialsPackage.DTE.StatusBar.Text = "Web Essentials: " + message;
        }

        private static DateTime GetLastRequestDate()
        {
            string logPath = GetLogFilePath();

            if (!File.Exists(logPath))
            {
                return new DateTime(2000, 1, 1);
            }

            return File.GetLastWriteTimeUtc(logPath);
        }

        private static string GetLogFilePath()
        {
            return Path.Combine(GetFolder(), "log.txt");
        }

        private static string GetFolder()
        {
            string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string folder = Path.Combine(user, _path);

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            return folder;
        }
    }
}