using System.IO;
using Microsoft.Win32;

namespace MadsKristensen.EditorExtensions
{
    internal static class IconRegistration
    {
        private static string _folder = GetFolder();

        public static void RegisterIcons()
        {
            using (RegistryKey classes = Registry.CurrentUser.OpenSubKey("SoftWare\\Classes", true))
            {
                if (classes != null)
                {
                    // IcedCoffeeScript
                    WriteKey(classes, ".iced", "CoffeeScript.ico");

                    // Markdown
                    WriteKey(classes, ".md", "Markdown.ico");
                    WriteKey(classes, ".mdown", "Markdown.ico");
                    WriteKey(classes, ".markdown", "Markdown.ico");
                    WriteKey(classes, ".mkd", "Markdown.ico");
                    WriteKey(classes, ".mkdn", "Markdown.ico");
                    WriteKey(classes, ".mdwn", "Markdown.ico");

                    // WebVTT
                    WriteKey(classes, ".vtt", "WebVTT.ico");
                }
            }
        }

        private static void WriteKey(RegistryKey classes, string extension, string iconName)
        {
            using (RegistryKey iced = classes.CreateSubKey(extension + "\\DefaultIcon"))
            {
                iced.SetValue(string.Empty, _folder + iconName);
            }
        }

        private static string GetFolder()
        {
            string directory =Path.GetDirectoryName( typeof(IconRegistration).Assembly.Location);
            return Path.Combine(directory, "Resources\\Icons\\");
        }
    }
}
