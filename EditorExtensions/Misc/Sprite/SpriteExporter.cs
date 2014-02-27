using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MadsKristensen.EditorExtensions
{
    internal class SpriteExporter
    {
        public static string Export(IEnumerable<SpriteFragment> fragments, string imageFile, ExportFormat format)
        {
            if (format == ExportFormat.Json)
            {
                return ExportJson(fragments, imageFile);
            }

            return ExportStylesheet(fragments, imageFile, format);
        }

        private static string ExportJson(IEnumerable<SpriteFragment> fragments, string imageFile)
        {
            var map = new
            {
                images = fragments.Select(fragment =>
                {
                    var item = new
                    {
                        Name = Path.GetFileName(fragment.FileName),
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

        private static string ExportStylesheet(IEnumerable<SpriteFragment> fragments, string imageFile, ExportFormat format)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(GetDescription(format));

            foreach (SpriteFragment fragment in fragments)
            {
                sb.AppendLine(GetSelector(fragment.FileName, format) + " {");
                sb.AppendLine("/* You may have to set 'display: block' */");
                sb.AppendLine("\twidth: " + fragment.Width + "px;");
                sb.AppendLine("\theight: " + fragment.Height + "px;");
                sb.AppendLine("\tbackground: url('" + Path.GetFileName(imageFile) + "') -" + fragment.X + "px -" + fragment.Y + "px;");
                sb.AppendLine("}");
            }

            string outputFile = GetFileName(imageFile, format);

            ProjectHelpers.CheckOutFileFromSourceControl(outputFile);
            File.WriteAllText(outputFile, sb.ToString().Replace("-0px", "0"), Encoding.UTF8);

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
            if (format == ExportFormat.Less)
                return ".sprite-" + Path.GetFileNameWithoutExtension(fileName) + "()";
            else if (format == ExportFormat.Scss)
                return "@mixin sprite-" + Path.GetFileNameWithoutExtension(fileName) + "()";

            return "." + Path.GetFileNameWithoutExtension(fileName);
        }

        private static string GetFileName(string imageFileName, ExportFormat format)
        {
            switch (format)
            {
                case ExportFormat.Css:
                    return imageFileName + ".css";
                case ExportFormat.Json:
                    return imageFileName + ".map";
                case ExportFormat.Less:
                    return imageFileName + ".less";
                case ExportFormat.Scss:
                    return imageFileName + ".scss";
            }

            return null;
        }
    }
}