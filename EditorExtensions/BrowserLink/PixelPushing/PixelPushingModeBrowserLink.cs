using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using EnvDTE;
using MadsKristensen.EditorExtensions.BrowserLink.UnusedCss;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Web.BrowserLink;
using Newtonsoft.Json;

namespace MadsKristensen.EditorExtensions.BrowserLink.PixelPushing
{
    [Export(typeof(IBrowserLinkExtensionFactory))]
    public class PixelPusingModeFactory : IBrowserLinkExtensionFactory
    {
        public BrowserLinkExtension CreateExtensionInstance(BrowserLinkConnection connection)
        {
            return new PixelPushingMode(connection);
        }

        public string GetScript()
        {
            using (Stream stream = GetType().Assembly.GetManifestResourceStream("MadsKristensen.EditorExtensions.BrowserLink.PixelPushing.PixelPushingModeBrowserLink.js"))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }

    public enum CssDeltaAction
    {
        Add,
        Reset,
        Update,
        Delete
    }

    public class PixelPusingCssDelta : IEquatable<PixelPusingCssDelta>
    {
        [JsonProperty]
        public string Url { get; set; }

        [JsonProperty]
        public int RuleIndex { get; set; }

        [JsonProperty]
        public CssDeltaAction Action { get; set; }

        [JsonProperty]
        public string Property { get; set; }

        [JsonProperty]
        public string NewValue { get; set; }

        [JsonProperty]
        public string Rule { get; set; }

        public bool Equals(PixelPusingCssDelta other)
        {
            return !ReferenceEquals(other, null) && other.Rule == Rule && other.Action == Action && other.Property == Property && other.RuleIndex == RuleIndex && other.NewValue == NewValue && other.Url == Url;
        }

        public override string ToString()
        {
            if (Action != CssDeltaAction.Delete)
            {
                return Action + ": \"" + Property + ":" + NewValue + "\" to " + Rule + " in " + Url;
            }
            
            return "Delete: \"" + Property + "\" from " + Rule + " in " + Url;
        }
    }

    public class PixelPushingMode : BrowserLinkExtension
    {
        private readonly UploadHelper _uploadHelper;

        private readonly BrowserLinkConnection _connection;

        public PixelPushingMode(BrowserLinkConnection connection)
        {
            _uploadHelper = new UploadHelper();
            _connection = connection;
        }

        public static string GetStyleSheetFileForUrl(string location, Project project)
        {
            //TODO: This needs to expand bundles, convert urls to local file names, and move from .min.css files to .css files where applicable
            //NOTE: Project parameter here is for the discovery of linked files, ones that might exist outside of the project structure
            var projectPath = project.Properties.Item("FullPath").Value.ToString();
            var projectUri = new Uri(projectPath, UriKind.Absolute);

            if (location == null)
            {
                return null;
            }

            var locationUri = new Uri(location, UriKind.RelativeOrAbsolute);

            //No absolute paths, unless they map into the same project
            if (locationUri.IsAbsoluteUri)
            {
                if (projectUri.IsBaseOf(locationUri))
                {
                    locationUri = locationUri.MakeRelativeUri(projectUri);
                }
                else
                {
                    //TODO: Fix this, it'll only work if the site is at the root of the server as is
                    locationUri = new Uri(locationUri.LocalPath, UriKind.Relative);
                }

                if (locationUri.IsAbsoluteUri)
                {
                    return null;
                }
            }

            var locationUrl = locationUri.ToString().TrimStart('/').ToLowerInvariant();

            //Hoist .min.css -> .css
            if (locationUrl.EndsWith(".min.css"))
            {
                locationUrl = locationUrl.Substring(0, locationUrl.Length - 8) + ".css";
            }

            locationUri = new Uri(locationUrl, UriKind.Relative);
            string filePath;

            try
            {
                Uri realLocation;
                if (Uri.TryCreate(projectUri, locationUri, out realLocation) && File.Exists(realLocation.LocalPath))
                {
                    //Try to move from .css -> .less
                    var lessFile = Path.ChangeExtension(realLocation.LocalPath, ".less");

                    if (File.Exists(lessFile))
                    {
                        locationUri = new Uri(lessFile, UriKind.Relative);
                        Uri.TryCreate(projectUri, locationUri, out realLocation);
                    }

                    filePath = realLocation.LocalPath;
                }
                else
                {
                    //Try to move from .min.css -> .less
                    var lessFile = Path.ChangeExtension(realLocation.LocalPath, ".less");

                    if (!File.Exists(lessFile))
                    {
                        return null;
                    }

                    locationUri = new Uri(lessFile, UriKind.Relative);
                    Uri.TryCreate(projectUri, locationUri, out realLocation);
                    filePath = realLocation.LocalPath;
                }
            }
            catch (IOException)
            {
                return null;
            }

            return filePath;
        }

        private static IStylingRule[] FlattenRules(IDocument document)
        {
            return document.Rules.OrderBy(x => x.Offset).ToArray();
        }

        private static readonly object DocumentUpdateSync = new object();

        private static void ProcessUpdate(ITextEdit edit, IStylingRule rule, PixelPusingCssDelta delta)
        {
            var children = rule.Source.Block.Children;
            Declaration decl;

            switch (delta.Action)
            {
                case CssDeltaAction.Add:
                    var startPosition = rule.Source.Block.Children.Last().Start; //Just before closing brace
                    var newDeclarationText = delta.Property + ": " + delta.NewValue + ";\r\n";
                    edit.Insert(startPosition, newDeclarationText);
                    break;
                case CssDeltaAction.Reset:
                case CssDeltaAction.Update:
                    decl = children.OfType<Declaration>().LastOrDefault(x => x.PropertyName.Text == delta.Property);
                    if (decl != null)
                    {
                        edit.Delete(decl.Values.TextStart, decl.Values.TextLength);
                        edit.Insert(decl.Values.TextStart, delta.NewValue);
                    }
                    break;
                case CssDeltaAction.Delete:
                    decl = children.OfType<Declaration>().LastOrDefault(x => x.PropertyName.Text == delta.Property);
                    if (decl != null)
                    {
                        edit.Delete(decl.Start, decl.Length);
                        var pos = decl.Start;

                        while (string.IsNullOrWhiteSpace(edit.Snapshot.GetText(--pos, 1)))
                        {
                            edit.Delete(pos, 1);
                        }
                    }
                    break;
            }
        }

        private static void UpdateSheets(string file, IEnumerable<PixelPusingCssDelta> set)
        {
            var doc = DocumentFactory.GetDocument(file, true);

            if (doc == null)
            {
                return;
            }

            doc.Reparse();

            var window = EditorExtensionsPackage.DTE.ItemOperations.OpenFile(file);
            window.Activate();
            var flattenedRules = FlattenRules(doc);
            var actions = new List<Tuple<IStylingRule, Action<ITextEdit>>>();

            foreach (var item in set)
            {
                var selectorName = RuleRegistry.StandardizeSelector(item.Rule);
                var matchingRules = flattenedRules.Where(x => string.Equals(x.CleansedSelectorName, selectorName, StringComparison.Ordinal)).OrderBy(x => x.Offset).ToList();

                if (item.RuleIndex >= matchingRules.Count)
                {
                    continue;
                }

                var matchingRule = matchingRules[item.RuleIndex];
                var localItem = item;
                actions.Add(new Tuple<IStylingRule, Action<ITextEdit>>(matchingRule, edit => ProcessUpdate(edit, matchingRule, localItem)));
            }

            var buffer = ProjectHelpers.GetCurentTextBuffer();
            var compositeEdit = buffer.CreateEdit();

            foreach (var action in actions.OrderByDescending(x => x.Item1.Offset).Select(x => x.Item2))
            {
                action(compositeEdit);
            }

            compositeEdit.Apply();

            using (CssSyncSuppressionContext.Get())
            {
                window.Document.Save();
            }
        }

        [BrowserLinkCallback]
        public void SyncCssSources(string operationId, string chunkContents, int chunkNumber, int chunkCount)
        {
            PixelPusingCssDelta[] result;
            var opId = Guid.Parse(operationId);
            if (_uploadHelper.TryFinishOperation(opId, chunkContents, chunkNumber, chunkCount, out result))
            {
                var urlGrouped = result.Where(x => x.Property != "selectorName" && x.Property != "selectorIndex").GroupBy(x => x.Url).ToList();

                lock (DocumentUpdateSync)
                {
                    foreach (var set in urlGrouped)
                    {
                        var file = GetStyleSheetFileForUrl(set.Key, _connection.Project);

                        if (file == null)
                        {
                            continue;
                        }

                        UpdateSheets(file, set);
                    }
                }
            }
        }

        public override void OnConnected(BrowserLinkConnection connection)
        {
            Browsers.Client(connection).Invoke("setPixelPusingMode", true, Guid.NewGuid().ToString());
        }
    }
}
