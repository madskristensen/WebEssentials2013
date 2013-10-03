using System;
using System.Collections.Concurrent;
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
        private static readonly ConcurrentDictionary<BrowserLinkConnection, PixelPushingMode> ExtensionByConnection = new ConcurrentDictionary<BrowserLinkConnection, PixelPushingMode>();
        private static readonly ConcurrentDictionary<string, bool> ContinuousSyncModeByProject = new ConcurrentDictionary<string, bool>();
        private readonly BrowserLinkConnection _connection;
        private readonly UploadHelper _uploadHelper;
        internal static bool IsPixelPushingModeEnabled = WESettings.GetBoolean(WESettings.Keys.PixelPushing_OnByDefault);
    
        public PixelPushingMode(BrowserLinkConnection connection)
        {
            ExtensionByConnection[connection] = this;
            _uploadHelper = new UploadHelper();
            _connection = connection;
        }

        internal static void All(Action<PixelPushingMode> method)
        {
            foreach (var extension in ExtensionByConnection.Values)
            {
                method(extension);
            }
        }

        public override IEnumerable<BrowserLinkAction> Actions
        {
            get { yield return new BrowserLinkAction("Pull Style Updates Now", PullStyleUpdates, PullStyleUpdatesBeforeQueryStatus);}
        }

        private static void PullStyleUpdatesBeforeQueryStatus(BrowserLinkAction browserLinkAction)
        {
            browserLinkAction.Enabled = IsPixelPushingModeEnabled;
        }

        private void PullStyleUpdates(BrowserLinkAction browserLinkAction)
        {
            Browsers.Client(_connection).Invoke("pullStyleData");
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
            SetMode();
        }

        private bool _isDisconnecting;

        public override void OnDisconnecting(BrowserLinkConnection connection)
        {
            PixelPushingMode extension;
            ExtensionByConnection.TryRemove(connection, out extension);
            _isDisconnecting = true;
            base.OnDisconnecting(connection);
        }

        private int _expectSequenceNumber;

        [BrowserLinkCallback]
        public async Task SyncCssRules(string operationId, string chunkContents, int chunkNumber, int chunkCount)
        {
            CssSelectorChangeData[][] result;
            var autoOpId = int.Parse(operationId);
            var opId = new Guid(autoOpId, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            if (_uploadHelper.TryFinishOperation(opId, chunkContents, chunkNumber, chunkCount, out result))
            {
                while (Volatile.Read(ref _expectSequenceNumber) != autoOpId && !_isDisconnecting)
                {
                    await Task.Delay(1);
                }

                if (_isDisconnecting)
                {
                    return;
                }

                using (CssSyncSuppressionContext.Get(excludeSpecificConnections: _connection))
                {
                    try
                    {
                        foreach (var logEntry in result)
                        {
                            var urlGrouped = logEntry.GroupBy(x => x.Url).ToList();
                            var tasks = new Task[urlGrouped.Count];
                            var index = 0;

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
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                    }
                    finally
                    {
                        Interlocked.Increment(ref _expectSequenceNumber);
                    }
                }
            }
        }

        private static IStylingRule[] FlattenRules(IDocument document)
        {
            return document.Rules.OrderBy(x => x.Offset).ToArray();
        }

        private static async Task UpdateSheetRulesAsync(string file, IEnumerable<CssSelectorChangeData> set)
        {
            ////Get off the UI thread
            //await Task.Factory.StartNew(() => { });
            var doc = DocumentFactory.GetDocument(file, true);
            if (doc == null)
            {
                return;
            }

            var oldSnapshotOnChange = doc.IsProcessingUnusedCssRules;
            doc.IsProcessingUnusedCssRules = false;
            var window = EditorExtensionsPackage.DTE.ItemOperations.OpenFile(file);
            window.Activate();
            var buffer = ProjectHelpers.GetCurentTextBuffer();
            doc.Reparse(buffer.CurrentSnapshot.GetText());
            var flattenedRules = FlattenRules(doc);

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

            //await Task.Delay(2000); //<-- Try to wait for the files to actually save
            doc.IsProcessingUnusedCssRules = oldSnapshotOnChange;
        }

        [BrowserLinkCallback]
        public void EnterContinuousSyncMode(bool value)
        {
            var changed = false;
            ContinuousSyncModeByProject.AddOrUpdate(_connection.Project.UniqueName, p => value, (p, x) =>
            {
                changed = x ^ value;
                return value;
            });

            if (changed)
            {
                All(x => x.SetMode());
            }
        }

        public void SetMode()
        {
            var continuousSyncMode = ContinuousSyncModeByProject.GetOrAdd(_connection.Project.UniqueName, p => false);
            Browsers.Client(_connection).Invoke("setPixelPusingMode", IsPixelPushingModeEnabled, continuousSyncMode);
        }
    }
}