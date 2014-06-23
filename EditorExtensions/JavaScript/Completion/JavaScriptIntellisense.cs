using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace MadsKristensen.EditorExtensions.JavaScript
{
    public static class JavaScriptIntellisense
    {
        public static void Register()
        {
            RegisterFile("resources\\scripts\\JsDocComments.js");
            RegisterFile("resources\\scripts\\Modern.Intellisense.js");
        }

        private static void RegisterFile(string path)
        {
            try
            {
                string userPath = GetUserFilePath(Path.GetFileName(path));
                string assembly = Assembly.GetExecutingAssembly().Location;
                string folder = Path.GetDirectoryName(assembly).ToLowerInvariant();
                string file = Path.Combine(folder, path);

                if (!File.Exists(file))
                    return;

                File.Copy(file, userPath, true);
                UpdateRegistry(userPath);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static string GetUserFilePath(string fileName)
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            return Path.Combine(folder, fileName);
        }

        private static void UpdateRegistry(string file)
        {
            string fileName = Path.GetFileName(file);

            // The APIs for setting the references are all internal, so we need to go directly through the registry.
            using (RegistryKey key = WebEssentialsPackage.Instance.UserRegistryRoot.OpenSubKey("JavaScriptLanguageService", true))
            {
                if (key == null)
                    return;

                string value = (string)key.GetValue("ReferenceGroups");
                if (value.Contains(file))
                    return;

                string newValue = value;
                int index = value.IndexOf(fileName, StringComparison.OrdinalIgnoreCase);

                if (index > -1)
                {
                    int start = value.LastIndexOf('|', index);
                    int length = index - start + fileName.Length;
                    string oldPath = value.Substring(start, length);
                    newValue = value.Replace(oldPath, "|" + file);
                }
                else
                {
                    int startWeb = value.IndexOf("Implicit (Web)", StringComparison.OrdinalIgnoreCase);

                    if (startWeb == -1)
                        return;

                    int semicolon = value.IndexOf(';', startWeb);

                    if (semicolon > -1)
                    {
                        newValue = value.Insert(semicolon, "|" + file);
                    }
                }

                key.SetValue("ReferenceGroups", newValue);
            }
        }
    }
}