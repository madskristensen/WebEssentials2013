﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using MadsKristensen.EditorExtensions.Settings;

namespace MadsKristensen.EditorExtensions
{
    public class BundleDocument : IBundleDocument
    {
        private bool isCss;

        public string FileName { get; set; }
        public IEnumerable<string> BundleAssets { get; set; }
        public IList<string> OriginalBundleAssets { get; private set; }
        public bool Minified { get; set; }
        public bool RunOnBuild { get; set; }
        public bool AdjustRelativePaths { get; set; }
        public string OutputDirectory { get; set; }

        public BundleDocument(string fileName, params string[] assets)
        {
            isCss = false;

            var extension = Path.GetExtension(assets.First()).TrimStart('.').ToLowerInvariant();

            IBundleSettings settings;

            if (extension == "css")
            {
                isCss = true;
                settings = WESettings.Instance.Css;
                AdjustRelativePaths = WESettings.Instance.Css.AdjustRelativePaths;
            }
            else if (extension == "html")
                settings = WESettings.Instance.Html;
            else
                settings = WESettings.Instance.JavaScript;

            FileName = fileName;
            BundleAssets = assets;
            Minified = settings.MakeMinified;
            RunOnBuild = settings.RunOnBuild;
            OutputDirectory = settings.OutputDirectory;
            OriginalBundleAssets = new List<string>();
        }

        public async Task<XDocument> WriteBundleRecipe()
        {
            string root = ProjectHelpers.GetRootFolder();

            if (string.IsNullOrEmpty(root))
                root = ProjectHelpers.GetProjectFolder(FileName);

            XmlWriterSettings settings = new XmlWriterSettings() { Indent = true };
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

            ProjectHelpers.CheckOutFileFromSourceControl(FileName);

            using (XmlWriter writer = await Task.Run(() => XmlWriter.Create(FileName, settings)))
            {
                XDocument doc = new XDocument(
                    new XElement("bundle",
                        new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                        new XAttribute(xsi + "noNamespaceSchemaLocation", "http://vswebessentials.com/schemas/v1/bundle.xsd"),
                        new XElement("settings",
                            new XComment("Determines if the bundle file should be automatically optimized after creation/update."),
                            new XElement("minify", Minified.ToString().ToLowerInvariant()),
                            new XComment("Determine whether to generate/re-generate this bundle on building the solution."),
                            new XElement("runOnBuild", RunOnBuild.ToString().ToLowerInvariant()),
                            new XComment("Specifies a custom subfolder to save files to. By default, compiled output will be placed in the same folder and nested under the original file."),
                            new XElement("outputDirectory", OutputDirectory)
                        ),
                        new XComment("The order of the <file> elements determines the order of the files in the bundle."),
                        new XElement("files", BundleAssets.Select(file => new XElement("file", GetRelativePath(file, root))))
                    )
                );

                if (isCss)
                    doc.Descendants("runOnBuild").FirstOrDefault().AddAfterSelf(
                        new XComment("Use absolute path in the generated CSS files. By default, the URLs are relative to generated bundled CSS file."),
                        new XElement("adjustRelativePaths", AdjustRelativePaths.ToString().ToLowerInvariant())
                    );

                doc.Save(writer);

                return doc;
            }
        }

        private string GetRelativePath(string file, string root)
        {
            file = "/" + FileHelpers.RelativePath(root, file);

            OriginalBundleAssets.Add(file);

            return file;
        }

        public async Task<IBundleDocument> LoadFromFile(string fileName)
        {
            return await BundleDocument.FromFile(fileName);
        }

        public static async Task<BundleDocument> FromFile(string fileName)
        {
            string root = ProjectHelpers.GetProjectFolder(fileName);
            string folder = Path.GetDirectoryName(root);

            if (folder == null || root == null)
                return null;

            XDocument doc = null;

            string contents = await FileHelpers.ReadAllTextRetry(fileName);

            try
            {
                doc = XDocument.Parse(contents);
            }
            catch (XmlException)
            {
                return null;
            }

            // Migrate old bundles
            doc = await MigrateBundle(doc, fileName, root, folder);

            if (doc == null)
                return null;

            XElement element = null;
            IEnumerable<string> rawConstituents = doc.Descendants("file").Select(s => s.Value);
            IEnumerable<string> constituentFiles = ProjectHelpers.GetBundleConstituentFiles(rawConstituents, root, folder, fileName);
            if (constituentFiles.Count() > 0)
            {

                BundleDocument bundle = new BundleDocument(fileName, constituentFiles.ToArray())
                {
                    OriginalBundleAssets = new List<string>(rawConstituents)
                };

                element = doc.Descendants("minify").FirstOrDefault();

                if (element != null)
                    bundle.Minified = element.Value.Equals("true", StringComparison.OrdinalIgnoreCase);

                element = doc.Descendants("runOnBuild").FirstOrDefault();

                if (element != null)
                    bundle.RunOnBuild = element.Value.Equals("true", StringComparison.OrdinalIgnoreCase);

                if (bundle.isCss)
                {
                    element = doc.Descendants("adjustRelativePaths").FirstOrDefault();

                    if (element != null)
                        bundle.AdjustRelativePaths = element.Value.Equals("true", StringComparison.OrdinalIgnoreCase);
                }

                element = doc.Descendants("outputDirectory").FirstOrDefault();

                if (element != null)
                    bundle.OutputDirectory = element.Value;

                return bundle;
            }
            else
            {
                return null;
            }
        }

        private static async Task<XDocument> MigrateBundle(XDocument doc, string fileName, string root, string folder)
        {
            string[] attrNames = new[] { "runOnBuild", "minify", "output" };
            XElement bundle = doc.Descendants("bundle").FirstOrDefault();
            string[] attributes = bundle.Attributes()
                                        .Where(a => attrNames.Contains(a.Name.ToString()))
                                        .Select(a => a.Name.ToString())
                                        .ToArray();

            if (attributes.Count() == 0)
                return doc;

            IEnumerable<string> constituentFiles = ProjectHelpers.GetBundleConstituentFiles(doc.Descendants("file").Select(s => s.Value), root, folder, fileName);
            if (constituentFiles.Count() > 0)
            {
                BundleDocument newDoc = new BundleDocument(fileName, constituentFiles.ToArray());

                if (attributes.Contains("runOnBuild"))
                    newDoc.RunOnBuild = bundle.Attribute("runOnBuild").Value.Equals("true", StringComparison.OrdinalIgnoreCase);

                if (attributes.Contains("minify"))
                    newDoc.Minified = bundle.Attribute("minify").Value.Equals("true", StringComparison.OrdinalIgnoreCase);

                return await newDoc.WriteBundleRecipe();
            }
            else
            {
                return null;
            }
        }
    }
}
