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
                    WriteKey(classes, "CoffeeScript.ico", ".iced");

                    // Markdown
                    WriteKey(classes, "Markdown.ico", ".md", ".mdown", ".markdown", ".mkd", ".mkdn", ".mdwn");

                    // WebVTT
                    WriteKey(classes, "WebVTT.ico", ".vtt");

                    // Bundles
                    WriteKey(classes, "Bundle.ico", ".bundle");

                    // Fonts
                    WriteKey(classes, "Font.ico", ".wof", ".woff", ".eot");

                    // Git
                    WriteKey(classes, "Git.ico", ".gitignore", ".gitattributes");
                }
            }
        }

        private static void WriteKey(RegistryKey classes, string iconName, params string[] extensions)
        {
            foreach (string extension in extensions)
            {
                using (RegistryKey iced = classes.CreateSubKey(extension + "\\DefaultIcon"))
                {
                    iced.SetValue(string.Empty, _folder + iconName);
                }
            }            
        }

        private static string GetFolder()
        {
            string directory =Path.GetDirectoryName( typeof(IconRegistration).Assembly.Location);
            return Path.Combine(directory, "Resources\\Icons\\");
        }
    }
}
