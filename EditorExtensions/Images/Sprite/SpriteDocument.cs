using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using MadsKristensen.EditorExtensions.Settings;

namespace MadsKristensen.EditorExtensions.Images
{
    internal class SpriteDocument : IBundleDocument
    {
        public string FileName { get; set; }
        public IEnumerable<string> BundleAssets { get; set; }
        public bool Optimize { get; set; }
        public bool IsVertical { get; set; }
        public bool RunOnBuild { get; set; }
        public string FileExtension { get; set; }
        public bool UseFullPathForIdentifierName { get; set; }
        public bool UseAbsoluteUrl { get; set; }
        public string CssOutputDirectory { get; set; }
        public string LessOutputDirectory { get; set; }
        public string ScssOutputDirectory { get; set; }

        public SpriteDocument(string fileName, params string[] imageFiles)
        {
            FileName = fileName;
            BundleAssets = imageFiles;
            FileExtension = Path.GetExtension(imageFiles.First()).TrimStart('.');
            Optimize = WESettings.Instance.Sprite.Optimize;
            IsVertical = WESettings.Instance.Sprite.IsVertical;
            RunOnBuild = WESettings.Instance.Sprite.RunOnBuild;
            UseFullPathForIdentifierName = WESettings.Instance.Sprite.UseFullPathForIdentifierName;
            UseAbsoluteUrl = WESettings.Instance.Sprite.UseAbsoluteUrl;
            CssOutputDirectory = WESettings.Instance.Sprite.CssOutputDirectory;
            LessOutputDirectory = WESettings.Instance.Sprite.LessOutputDirectory;
            ScssOutputDirectory = WESettings.Instance.Sprite.ScssOutputDirectory;
        }

        public async Task WriteSpriteRecipe()
        {
            string root = ProjectHelpers.GetRootFolder();
            XmlWriterSettings settings = new XmlWriterSettings() { Indent = true };
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

            using (XmlWriter writer = await Task.Run(() => XmlWriter.Create(FileName, settings)))
            {
                new XDocument(
                    new XElement("sprite",
                        new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                        new XAttribute(xsi + "noNamespaceSchemaLocation", "http://vswebessentials.com/schemas/v1/sprite.xsd"),
                        new XElement("settings",
                            new XComment("Determines if the sprite image should be automatically optimized after creation/update."),
                            new XElement("optimize", Optimize.ToString().ToLowerInvariant()),
                            new XComment("Determines the orientation of images to form this sprite. The value must be vertical or horizontal."),
                            new XElement("orientation", IsVertical ? "vertical" : "horizontal"),
                            new XComment("File extension of sprite image."),
                            new XElement("outputType", FileExtension.ToString().ToLowerInvariant()),
                            new XComment("Determin whether to generate/re-generate this sprite on building the solution."),
                            new XElement("runOnBuild", RunOnBuild.ToString().ToLowerInvariant()),
                            new XComment("Use full path to generate unique class or mixin name in CSS, LESS and SASS files. Consider disabling this if you want class names to be filename only."),
                            new XElement("fullPathForIdentifierName", UseFullPathForIdentifierName.ToString().ToLowerInvariant()),
                            new XComment("Use absolute path in the generated CSS-like files. By default, the URLs are relative to sprite image file (and the location of CSS, LESS and SCSS)."),
                            new XElement("useAbsoluteUrl", UseAbsoluteUrl.ToString().ToLowerInvariant()),
                            new XComment("Specifies a custom subfolder to save CSS files to. By default, compiled output will be placed in the same folder and nested under the original file."),
                            new XElement("outputDirectoryForCss", CssOutputDirectory),
                            new XComment("Specifies a custom subfolder to save LESS files to. By default, compiled output will be placed in the same folder and nested under the original file."),
                            new XElement("outputDirectoryForLess", LessOutputDirectory),
                            new XComment("Specifies a custom subfolder to save SCSS files to. By default, compiled output will be placed in the same folder and nested under the original file."),
                            new XElement("outputDirectoryForScss", ScssOutputDirectory)
                        ),
                        new XComment("The order of the <file> elements determines the order of the images in the sprite."),
                        new XElement("files", BundleAssets.Select(file => new XElement("file", "/" + FileHelpers.RelativePath(root, file))))
                    )
                ).Save(writer);
            }
        }

        public static SpriteDocument FromFile(string fileName)
        {
            string root = ProjectHelpers.GetProjectFolder(fileName);
            string folder = Path.GetDirectoryName(root);

            if (folder == null || root == null)
                return null;

            XDocument doc = null;

            try
            {
                doc = XDocument.Load(fileName);
            }
            catch (XmlException)
            {
                return null;
            }

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

            element = doc.Descendants("runOnBuild").FirstOrDefault();

            if (element != null)
                sprite.RunOnBuild = element.Value.Equals("true", StringComparison.OrdinalIgnoreCase);

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
