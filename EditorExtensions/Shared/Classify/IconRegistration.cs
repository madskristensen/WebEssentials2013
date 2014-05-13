using System.IO;
using MadsKristensen.EditorExtensions.CoffeeScript;
using MadsKristensen.EditorExtensions.JavaScript;
using MadsKristensen.EditorExtensions.TypeScript;
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
                if (classes == null)
                    return;

                // IcedCoffeeScript
                AddIcon(classes, "CoffeeScript.ico", ".iced");

                // LiveScript
                AddIcon(classes, "LiveScript.ico", ".ls", ".livescript", ".lsc");

                // Markdown
                AddIcon(classes, "Markdown.ico", ".md", ".mdown", ".markdown", ".mkd", ".mkdn", ".mdwn", ".mmd");

                // WebVTT
                AddIcon(classes, "WebVTT.ico", ".vtt");

                // Bundles
                AddIcon(classes, "Bundle.ico", ".bundle");

                // Fonts
                AddIcon(classes, "Font.ico", ".wof", ".woff", ".eot");

                // Git
                AddIcon(classes, "Git.ico", ".gitignore", ".gitattributes");

                // Generic script
                AddIcon(classes, "GenericScript.ico", ".appcache", JsHintCompiler.ConfigFileName, ".jshintignore", TsLintCompiler.ConfigFileName, JsCodeStyleCompiler.ConfigFileName, CoffeeLintCompiler.ConfigFileName, ".sjs", ".jsonld", ".bowerrc");

                // Jigsaw
                AddIcon(classes, "Jigsaw.ico", ".sprite");
            }
        }

        private static void AddIcon(RegistryKey classes, string iconName, params string[] extensions)
        {
            foreach (string extension in extensions)
            {
                using (RegistryKey key = classes.CreateSubKey(extension + "\\DefaultIcon"))
                {
                    key.SetValue(string.Empty, _folder + iconName);
                }
            }
        }

        private static string GetFolder()
        {
            string directory = Path.GetDirectoryName(typeof(IconRegistration).Assembly.Location);
            return Path.Combine(directory, "Resources\\Icons\\");
        }
    }
}
