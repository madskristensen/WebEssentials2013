using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MadsKristensen.EditorExtensions.Images
{
    internal class SpriteExporter
    {
        public async static Task<string> Export(IEnumerable<SpriteFragment> fragments, SpriteDocument sprite, string imageFile, ExportFormat format)
        {
            if (format == ExportFormat.Json)
            {
                return ExportJson(fragments, sprite, imageFile);
            }

            return await ExportStylesheet(fragments, sprite, imageFile, format);
        }

        private static string ExportJson(IEnumerable<SpriteFragment> fragments, SpriteDocument sprite, string imageFile)
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

            string outputFile = GetFileName(imageFile, sprite, ExportFormat.Json);
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

        private async static Task<string> ExportStylesheet(IEnumerable<SpriteFragment> fragments, SpriteDocument sprite, string imageFile, ExportFormat format)
        {
            string outputFile = GetFileName(imageFile, sprite, format);
            var outputDirectory = Path.GetDirectoryName(outputFile);
            StringBuilder sb = new StringBuilder().AppendLine(GetDescription(format));
            string root = ProjectHelpers.GetRootFolder();

            foreach (SpriteFragment fragment in fragments)
            {
                var rootAbsoluteUrl = FileHelpers.RelativePath(root, fragment.FileName);

                var bgUrl = sprite.UseAbsoluteUrl ? "/" + FileHelpers.RelativePath(root, imageFile) : FileHelpers.RelativePath(outputFile, imageFile);

                sb.AppendLine(GetSelector(rootAbsoluteUrl, sprite, format) + " {");
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

        private static string GetSelector(string fileName, SpriteDocument sprite, ExportFormat format)
        {
            string className = FileHelpers.GetFileNameWithoutExtension(fileName);

            if (sprite.UseFullPathForIdentifierName)
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

        private static string GetFileName(string imageFileName, SpriteDocument sprite, ExportFormat format)
        {
            switch (format)
            {
                case ExportFormat.Css:
                    if (sprite.CssOutputDirectory != null)
                        return imageFileName = ProjectHelpers.GetAbsolutePathFromSettings(sprite.CssOutputDirectory, imageFileName + ".css");
                    return imageFileName + ".css";
                case ExportFormat.Less:
                    if (sprite.CssOutputDirectory != null)
                        return imageFileName = ProjectHelpers.GetAbsolutePathFromSettings(sprite.LessOutputDirectory, imageFileName + ".less");
                    return imageFileName + ".less";
                case ExportFormat.Scss:
                    if (sprite.CssOutputDirectory != null)
                        return imageFileName = ProjectHelpers.GetAbsolutePathFromSettings(sprite.ScssOutputDirectory, imageFileName + ".scss");
                    return imageFileName + ".scss";
                case ExportFormat.Json:
                    return imageFileName + ".map";
            }

            return null;
        }
    }
}