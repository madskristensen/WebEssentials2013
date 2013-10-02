using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using MadsKristensen.EditorExtensions.BrowserLink.UnusedCss;
using Microsoft.VisualStudio.Web.BrowserLink;

namespace MadsKristensen.EditorExtensions.BrowserLink.PixelPushing
{
    public class PixelPushingMode : BrowserLinkExtension
    {
        private static readonly ReaderWriterLockSlim ProcessingGate = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly BrowserLinkConnection _connection;
        private readonly UploadHelper _uploadHelper;

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

        public override void OnConnected(BrowserLinkConnection connection)
        {
            Browsers.Client(connection).Invoke("setPixelPusingMode", true, Guid.NewGuid().ToString());
        }

        [BrowserLinkCallback]
        public async Task SyncCssRules(string operationId, string chunkContents, int chunkNumber, int chunkCount)
        {
            CssSelectorChangeData[] result;
            var opId = Guid.Parse(operationId);
            if (_uploadHelper.TryFinishOperation(opId, chunkContents, chunkNumber, chunkCount, out result))
            {
                using (CssSyncSuppressionContext.Get())
                {
                    var urlGrouped = result.GroupBy(x => x.Url).ToList();
                    var tasks = new Task[urlGrouped.Count];
                    var index = 0;
                    ProcessingGate.EnterWriteLock();

                    foreach (var set in urlGrouped)
                    {
                        var file = GetStyleSheetFileForUrl(set.Key, _connection.Project);

                        if (file == null)
                        {
                            continue;
                        }

                        tasks[index++] = UpdateSheetRulesAsync(file, set);
                    }

                    await Task.WhenAll(tasks);
                    await Task.Delay(1000); //<-- Try to wait for the files to actually save
                    ProcessingGate.ExitWriteLock();
                }
            }
        }

        private static IStylingRule[] FlattenRules(IDocument document)
        {
            return document.Rules.OrderBy(x => x.Offset).ToArray();
        }

        private static async Task UpdateSheetRulesAsync(string file, IEnumerable<CssSelectorChangeData> set)
        {
            //Get off the UI thread
            await Task.Factory.StartNew(() => { });
                var doc = DocumentFactory.GetDocument(file, true);

                if (doc == null)
                {
                    return;
                }

                doc.Reparse();
                var window = EditorExtensionsPackage.DTE.ItemOperations.OpenFile(file);
                window.Activate();
                window.Document.Save();
                var flattenedRules = FlattenRules(doc);

                var buffer = ProjectHelpers.GetCurentTextBuffer();
                var allEdits = new List<CssRuleBlockSyncAction>();

                foreach (var item in set)
                {
                    var selectorName = RuleRegistry.StandardizeSelector(item.Rule);
                    var matchingRules = flattenedRules.Where(x => string.Equals(x.CleansedSelectorName, selectorName, StringComparison.Ordinal)).OrderBy(x => x.Offset).ToList();
                    var rule = matchingRules[item.RuleIndex];
                    var actions = await CssRuleDefinitionSync.ComputeSyncActionsAsync(rule.Source, item.NewValue, item.OldValue);
                    allEdits.AddRange(actions);
                }

                var compositeEdit = buffer.CreateEdit();

                try
                {
                    foreach (var action in allEdits)
                    {
                        action(window, compositeEdit);
                    }
                }
                catch
                {
                    compositeEdit.Cancel();
                }
                finally
                {
                    if (!compositeEdit.Canceled)
                    {
                        compositeEdit.Apply();
                        EditorExtensionsPackage.DTE.ExecuteCommand("Edit.FormatDocument");
                        window.Document.Save();
                    }
            }
        }
    }
}