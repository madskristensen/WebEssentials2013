using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MadsKristensen.EditorExtensions.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MadsKristensen.EditorExtensions.Images
{
    internal class SpriteExporter
    {
        public async static Task<string> Export(IEnumerable<SpriteFragment> fragments, string imageFile, ExportFormat format)
        {
            if (format == ExportFormat.Json)
            {
                return ExportJson(fragments, imageFile);
            }

            return await ExportStylesheet(fragments, imageFile, format);
        }

        private static string ExportJson(IEnumerable<SpriteFragment> fragments, string imageFile)
        {
            string root = ProjectHelpers.GetRootFolder();

            var map = new
            {
                images = fragments.Select(fragment =>
                {
                    var item = new
                    {
                        Name = "/" + FileHelpers.RelativePath(root, fragment.FileName),
                        Width = fragment.Width,
                        Height = fragment.Height,
                        OffsetX = fragment.X,
                        OffsetY = fragment.Y,
                    };

                    return item;
                })
            };

            string outputFile = GetFileName(imageFile, ExportFormat.Json);
            ProjectHelpers.CheckOutFileFromSourceControl(outputFile);

            using (StreamWriter sw = new StreamWriter(outputFile))
            using (JsonWriter jw = new JsonTextWriter(sw))
            {
                jw.Formatting = Formatting.Indented;

                var serializer = new JsonSerializer();
                serializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
                serializer.Serialize(jw, map);
            }

            return outputFile;
        }

        private async static Task<string> ExportStylesheet(IEnumerable<SpriteFragment> fragments, string imageFile, ExportFormat format)
        {
            string outputFile = GetFileName(imageFile, format);
            var outputDirectory = Path.GetDirectoryName(outputFile);
            StringBuilder sb = new StringBuilder().AppendLine(GetDescription(format));
            string root = ProjectHelpers.GetRootFolder();

            foreach (SpriteFragment fragment in fragments)
            {
                var rootAbsoluteUrl = FileHelpers.RelativePath(root, fragment.FileName);

                var bgUrl = WESettings.Instance.Sprite.UseAbsoluteUrl ? "/" + FileHelpers.RelativePath(root, imageFile) : FileHelpers.RelativePath(outputFile, imageFile);

                sb.AppendLine(GetSelector(rootAbsoluteUrl, format) + " {");
                sb.AppendLine("/* You may have to set 'display: block' */");
                sb.AppendLine("\twidth: " + fragment.Width + "px;");
                sb.AppendLine("\theight: " + fragment.Height + "px;");
                sb.AppendLine("\tbackground: url('" + bgUrl + "') -" + fragment.X + "px -" + fragment.Y + "px;");
                sb.AppendLine("}");
            }

            bool IsExists = System.IO.Directory.Exists(outputDirectory);
            if (!IsExists)
                System.IO.Directory.CreateDirectory(outputDirectory);

            ProjectHelpers.CheckOutFileFromSourceControl(outputFile);
            await FileHelpers.WriteAllTextRetry(outputFile, sb.ToString().Replace("-0px", "0"));

            return outputFile;
        }

        private static string GetDescription(ExportFormat format)
        {
            string text = "This is an example of how to use the image sprite in your own CSS files";

            if (format != ExportFormat.Css)
                text = "@import this file directly into your existing " + format + " files to use these mixins";

            return "/*" + Environment.NewLine + text + Environment.NewLine + "*/";
        }

        private static string GetSelector(string fileName, ExportFormat format)
        {
            string className = FileHelpers.GetFileNameWithoutExtension(fileName);

            if (WESettings.Instance.Sprite.UseFullPathForNamingIdentifier)
            {
                string withoutExtensionWithDirectoryName = Path.Combine(Path.GetDirectoryName(fileName), className);
                className = string.Join("-", withoutExtensionWithDirectoryName.Split(
                                             new[] { Path.DirectorySeparatorChar,
                                                     Path.AltDirectorySeparatorChar }));
            }

            if (format == ExportFormat.Less)
                return ".sprite-" + className + "()";
            else if (format == ExportFormat.Scss)
                return "@mixin sprite-" + className + "()";

            return "." + className;
        }

        private static string GetFileName(string imageFileName, ExportFormat format)
        {
            switch (format)
            {
                case ExportFormat.Css:
                    if (WESettings.Instance.Sprite.CssOutputDirectory != null)
                        return imageFileName = GetAbsolutePathFromSettings(WESettings.Instance.Sprite.CssOutputDirectory, imageFileName, ".css");
                    return imageFileName + ".css";
                case ExportFormat.Less:
                    if (WESettings.Instance.Sprite.CssOutputDirectory != null)
                        return imageFileName = GetAbsolutePathFromSettings(WESettings.Instance.Sprite.LessOutputDirectory, imageFileName, ".less");
                    return imageFileName + ".less";
                case ExportFormat.Scss:
                    if (WESettings.Instance.Sprite.CssOutputDirectory != null)
                        return imageFileName = GetAbsolutePathFromSettings(WESettings.Instance.Sprite.ScssOutputDirectory, imageFileName, ".scss");
                    return imageFileName + ".scss";
                case ExportFormat.Json:
                    return imageFileName + ".map";
            }

            return null;
        }

        private static string GetAbsolutePathFromSettings(string settingsPath, string imagePath, string ext)
        {
            if (string.IsNullOrEmpty(settingsPath))
                return imagePath + ext;

            string targetFileName = Path.GetFileName(imagePath + ext);
            string sourceDir = Path.GetDirectoryName(imagePath);

            // If the output path is not project-relative, combine it directly.
            if (!settingsPath.StartsWith("~/", StringComparison.OrdinalIgnoreCase)
             && !settingsPath.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                return Path.GetFullPath(Path.Combine(sourceDir, settingsPath, targetFileName));

            string rootDir = ProjectHelpers.GetRootFolder();

            if (string.IsNullOrEmpty(rootDir))
                // If no project is loaded, assume relative to file anyway
                rootDir = sourceDir;

            return Path.GetFullPath(Path.Combine(
                rootDir,
                settingsPath.TrimStart('~', '/'),
                targetFileName
            ));
        }
    }
}