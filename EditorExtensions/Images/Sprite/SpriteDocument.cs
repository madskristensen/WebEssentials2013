using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using MadsKristensen.EditorExtensions.Settings;

namespace MadsKristensen.EditorExtensions.Images
{
    internal class SpriteDocument
    {
        public string FileName { get; set; }
        public IEnumerable<string> ImageFiles { get; set; }
        public bool Optimize { get; set; }
        public bool IsVertical { get; set; }
        public string FileExtension { get; set; }
        public bool UseFullPathForIdentifierName { get; set; }
        public bool UseAbsoluteUrl { get; set; }
        public string CssOutputDirectory { get; set; }
        public string LessOutputDirectory { get; set; }
        public string ScssOutputDirectory { get; set; }

        public SpriteDocument(string fileName, params string[] imageFiles)
        {
            FileName = fileName;
            ImageFiles = imageFiles;
            Optimize = true;
            IsVertical = true;
            FileExtension = Path.GetExtension(imageFiles.First()).TrimStart('.');
            UseFullPathForIdentifierName = WESettings.Instance.Sprite.UseFullPathForIdentifierName;
            UseAbsoluteUrl = WESettings.Instance.Sprite.UseAbsoluteUrl;
            CssOutputDirectory = WESettings.Instance.Sprite.CssOutputDirectory;
            LessOutputDirectory = WESettings.Instance.Sprite.LessOutputDirectory;
            ScssOutputDirectory = WESettings.Instance.Sprite.ScssOutputDirectory;
        }

        public void Save()
        {
            XmlWriterSettings settings = new XmlWriterSettings() { Indent = true };

            using (XmlWriter writer = XmlWriter.Create(FileName, settings))
            {
                writer.WriteStartElement("sprite");
                writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
                writer.WriteAttributeString("xsi", "noNamespaceSchemaLocation", null, "http://vswebessentials.com/schemas/v1/sprite.xsd");

                // Settings
                writer.WriteStartElement("settings");
                writer.WriteComment("Determines if the sprite image should be automatically optimized after creation/update");
                writer.WriteElementString("optimize", Optimize ? "true" : "false");
                writer.WriteElementString("orientation", IsVertical ? "vertical" : "horizontal");
                writer.WriteElementString("outputType", FileExtension.ToString().ToLowerInvariant());
                writer.WriteComment("Use full path to generate unique class or mixin name in CSS, LESS and SASS files. Consider disabling this if you want class names to be filename only.");
                writer.WriteElementString("fullPathForIdentifierName", UseFullPathForIdentifierName ? "true" : "false");
                writer.WriteComment("Use absolute path in the generated CSS-like files. By default, the URLs are relative to sprite image file (and the location of CSS, LESS and SCSS).");
                writer.WriteElementString("useAbsoluteUrl", UseAbsoluteUrl ? "true" : "false");
                writer.WriteComment("Specifies a custom subfolder to save CSS files to. By default, compiled output will be placed in the same folder and nested under the original file.");
                writer.WriteElementString("outputDirectoryForCss", CssOutputDirectory);
                writer.WriteComment("Specifies a custom subfolder to save LESS files to. By default, compiled output will be placed in the same folder and nested under the original file.");
                writer.WriteElementString("outputDirectoryForLess", LessOutputDirectory);
                writer.WriteComment("Specifies a custom subfolder to save SCSS files to. By default, compiled output will be placed in the same folder and nested under the original file.");
                writer.WriteElementString("outputDirectoryForScss", ScssOutputDirectory);
                writer.WriteEndElement(); // </settings>

                // Files
                writer.WriteComment("The order of the <file> elements determines the order of the images in the sprite.");
                writer.WriteStartElement("files");

                string root = ProjectHelpers.GetRootFolder();

                foreach (string file in ImageFiles)
                {
                    string relative = "/" + FileHelpers.RelativePath(root, file);
                    writer.WriteElementString("file", relative);
                }

                writer.WriteEndElement(); // </files>
                writer.WriteEndElement(); // </sprite>
            }
        }

        public static SpriteDocument FromFile(string fileName)
        {
            XDocument doc = XDocument.Load(fileName);
            string root = ProjectHelpers.GetProjectFolder(fileName);
            string folder = Path.GetDirectoryName(root);
            XElement element = null;

            var imageFiles = from f in doc.Descendants("file")
                             select ProjectHelpers.ToAbsoluteFilePath(f.Value, root, folder);

            SpriteDocument sprite = new SpriteDocument(fileName, imageFiles.ToArray());

            element = doc.Descendants("optimize").FirstOrDefault();

            if (element != null)
                sprite.Optimize = element.Value.Equals("true", StringComparison.OrdinalIgnoreCase);

            element = doc.Descendants("orientation").FirstOrDefault();

            if (element != null)
                sprite.IsVertical = element.Value.Equals("vertical", StringComparison.OrdinalIgnoreCase);

            element = doc.Descendants("outputType").FirstOrDefault();

            if (element != null)
                sprite.FileExtension = element.Value;

            element = doc.Descendants("fullPathForIdentifierName").FirstOrDefault();

            if (element != null)
                sprite.UseFullPathForIdentifierName = element.Value.Equals("true", StringComparison.OrdinalIgnoreCase);

            element = doc.Descendants("useAbsoluteUrl").FirstOrDefault();

            if (element != null)
                sprite.UseAbsoluteUrl = element.Value.Equals("true", StringComparison.OrdinalIgnoreCase);

            element = doc.Descendants("outputDirectoryForCss").FirstOrDefault();

            if (element != null)
                sprite.CssOutputDirectory = element.Value;

            element = doc.Descendants("outputDirectoryForLess").FirstOrDefault();

            if (element != null)
                sprite.LessOutputDirectory = element.Value;

            element = doc.Descendants("outputDirectoryForScss").FirstOrDefault();

            if (element != null)
                sprite.ScssOutputDirectory = element.Value;

            return sprite;
        }
    }
}