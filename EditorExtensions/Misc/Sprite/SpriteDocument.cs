using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace MadsKristensen.EditorExtensions
{
    internal class SpriteDocument
    {
        public SpriteDocument(string fileName, params string[] imageFiles)
        {
            FileName = fileName;
            ImageFiles = imageFiles;
            Optimize = true;
            IsVertical = true;
            FileExtension = Path.GetExtension(imageFiles.First()).TrimStart('.');
        }

        public string FileName { get; set; }
        public IEnumerable<string> ImageFiles { get; set; }
        public bool Optimize { get; set; }
        public bool IsVertical { get; set; }
        public string FileExtension { get; set; }

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

            var imageFiles = from f in doc.Descendants("file")
                             select ProjectHelpers.ToAbsoluteFilePath(f.Value, root, folder);

            SpriteDocument sprite = new SpriteDocument(fileName, imageFiles.ToArray());
            sprite.Optimize = doc.Descendants("optimize").First().Value.Equals("true", StringComparison.OrdinalIgnoreCase);
            sprite.IsVertical = doc.Descendants("orientation").First().Value.Equals("vertical", StringComparison.OrdinalIgnoreCase);
            sprite.FileExtension = doc.Descendants("outputType").First().Value;

            return sprite;
        }
    }
}