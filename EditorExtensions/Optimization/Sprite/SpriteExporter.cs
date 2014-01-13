using System;
using System.IO;
using System.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class SpriteExporter
    {
        public void ExportToCodeFile(Sprite sprite, string fileName, SpriteFormatter format)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(GetDescription(format));

            foreach (var image in sprite.MappedImages)
            {
                ImageInfo info = image.ImageInfo as ImageInfo;
                string path = FileHelpers.RelativePath(fileName, info.Name);

                sb.AppendLine(GetSelector(info.Name, format) + " {");
                sb.AppendLine("\twidth: " + info.Width + "px;");
                sb.AppendLine("\theight: " + info.Height + "px;");
                sb.AppendLine("\tbackground: url('" + path + "') " + image.X + "px " + image.Y + "px;");
                sb.AppendLine("}");
            }

            string outputFileName = fileName + "." + format.ToString().ToLowerInvariant();
            File.WriteAllText(outputFileName, sb.ToString());
        }

        private static string GetDescription(SpriteFormatter format)
        {
            string text = "This is an example of how to use the image sprite in your own CSS files";

            if (format != SpriteFormatter.CSS)
                text = "@import this file directly into your existing "+ format +" files to use these mixins";

            return "/*" + Environment.NewLine + text + Environment.NewLine + "*/";
        }

        private static string GetSelector(string fileName, SpriteFormatter format)
        {
            if (format == SpriteFormatter.LESS)
                return ".sprite-" + Path.GetFileNameWithoutExtension(fileName) + "()";
            else if (format == SpriteFormatter.SCSS)
                return "@mixin sprite-" + Path.GetFileNameWithoutExtension(fileName) + "()";

            return "." + Path.GetFileNameWithoutExtension(fileName);
        }
    }

    public enum SpriteFormatter
    {
        CSS,
        LESS,
        SCSS
    }
}