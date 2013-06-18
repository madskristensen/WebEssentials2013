using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;

namespace MadsKristensen.EditorExtensions
{
    public static class JsDocComments
    {
        private static string _fileName = "JsDocComments.js";

        public static void Register()
        {
            try
            {
                string userPath = GetUserFilePath();

                if (!File.Exists(userPath))
                {
                    string assembly = Assembly.GetExecutingAssembly().Location;
                    string folder = Path.GetDirectoryName(assembly).ToLowerInvariant();
                    string file = Path.Combine(folder, "resources\\scripts\\JsDocComments.js");

                    if (!File.Exists(file))
                        return;

                    File.Copy(file, userPath);
                    UpdateRegistry(userPath);
                }
            }
            catch
            {
                Logger.Log("Error registering JSDoc comments with Visual Studio");
            }
        }

        private static string GetUserFilePath()
        {
            string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string folder = Path.Combine(user, "Web Essentials");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            return Path.Combine(folder, _fileName);
        }

        private static void UpdateRegistry(string file)
        {
            using (RegistryKey key = EditorExtensionsPackage.Instance.UserRegistryRoot.OpenSubKey("JavaScriptLanguageService", true))
            {
                if (key != null)
                {
                    string value = (string)key.GetValue("ReferenceGroups");
                    if (value.Contains(file))
                        return;

                    string newValue = value;
                    int index = value.IndexOf(_fileName);

                    if (index > -1)
                    {
                        int start = value.LastIndexOf('|', index);
                        int length = index - start + _fileName.Length;
                        string oldPath = value.Substring(start, length);
                        newValue = value.Replace(oldPath, "|" + file);
                    }
                    else
                    {
                        int startWeb = value.IndexOf("Implicit (Web)");
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
}