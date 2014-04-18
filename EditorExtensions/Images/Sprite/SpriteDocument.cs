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
        public bool UseFullPathForNamingIdentifier { get; set; }
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
            UseFullPathForNamingIdentifier = WESettings.Instance.Sprite.UseFullPathForNamingIdentifier;
            UseAbsoluteUrl = WESettings.Instance.Sprite.UseAbsoluteUrl;
            CssOutputDirectory = WESettings.Instance.Sprite.CssOutputDirectory;
            LessOutputDirectory = WESettings.Instance.Sprite.LessOutputDirectory;
            ScssOutputDirectory = WESettings.Instance.Sprite.ScssOutputDirectory;
        }

        public void Save()
        {
            XmlWriterSettings settings = new XmlWriterSettings() { WriteEndDocumentOnClose = false, Indent = true };

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
                writer.WriteElementString("UseFullPathForNamingIdentifier", UseFullPathForNamingIdentifier ? "true" : "false");
                writer.WriteElementString("UseAbsoluteUrl", UseAbsoluteUrl ? "true" : "false");
                writer.WriteElementString("CssOutputDirectory", CssOutputDirectory);
                writer.WriteElementString("LessOutputDirectory", LessOutputDirectory);
                writer.WriteElementString("ScssOutputDirectory", ScssOutputDirectory);
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

            element = doc.Descendants("UseFullPathForNamingIdentifier").FirstOrDefault();

            if (element != null)
                sprite.UseFullPathForNamingIdentifier = element.Value.Equals("true", StringComparison.OrdinalIgnoreCase);

            element = doc.Descendants("UseAbsoluteUrl").FirstOrDefault();

            if (element != null)
                sprite.UseAbsoluteUrl = element.Value.Equals("true", StringComparison.OrdinalIgnoreCase);

            element = doc.Descendants("CssOutputDirectory").FirstOrDefault();

            if (element != null)
                sprite.CssOutputDirectory = element.Value;

            element = doc.Descendants("LessOutputDirectory").FirstOrDefault();

            if (element != null)
                sprite.LessOutputDirectory = element.Value;

            element = doc.Descendants("ScssOutputDirectory").FirstOrDefault();

            if (element != null)
                sprite.ScssOutputDirectory = element.Value;

            return sprite;
        }
    }
}