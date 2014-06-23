using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using MadsKristensen.EditorExtensions.BrowserLink.UnusedCss;
using MadsKristensen.EditorExtensions.Settings;
using Microsoft.VisualStudio.Web.BrowserLink;

namespace MadsKristensen.EditorExtensions.BrowserLink.PixelPushing
{
    public class PixelPushingMode : BrowserLinkExtension
    {
        private static readonly ConcurrentDictionary<BrowserLinkConnection, PixelPushingMode> ExtensionByConnection = new ConcurrentDictionary<BrowserLinkConnection, PixelPushingMode>();
        private static readonly ConcurrentDictionary<string, bool> ContinuousSyncModeByProject = new ConcurrentDictionary<string, bool>();
        private readonly BrowserLinkConnection _connection;
        private readonly UploadHelper _uploadHelper;
        private static List<Regex> _ignoreList;
        private bool _isDisconnecting;
        private int _expectSequenceNumber;

        public override IEnumerable<BrowserLinkAction> Actions
        {
            get
            {
                yield return new BrowserLinkAction("Save F12 Changes", PullStyleUpdates, PullStyleUpdatesBeforeQueryStatus);
                yield return new BrowserLinkAction("F12 Auto-Sync", AutoSyncStyleUpdates, AutoSyncStyleUpdatesBeforeQueryStatus);
            }
        }
        public static bool IsPixelPushingModeEnabled
        {
            get { return WESettings.Instance.BrowserLink.EnablePixelPushing; }
            set
            {
                if (value == IsPixelPushingModeEnabled)
                    return;
                WESettings.Instance.BrowserLink.EnablePixelPushing = value;
                SettingsStore.Save();
            }
        }
        public static IEnumerable<Regex> IgnoreList
        {
            get
            {
                var ignorePatterns = WESettings.Instance.BrowserLink.CssIgnorePatterns ?? "";

                return _ignoreList ?? (_ignoreList = ignorePatterns.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => new Regex(UnusedCssExtension.FilePatternToRegex(x.Trim()))).ToList());
            }
        }

        public PixelPushingMode(BrowserLinkConnection connection)
        {
            ExtensionByConnection[connection] = this;
            _uploadHelper = new UploadHelper();
            _connection = connection;
            Settings.BrowserLinkOptions.SettingsUpdated += (sender, args) => _ignoreList = null;
        }

        internal static void All(Action<PixelPushingMode> method)
        {
            foreach (var extension in ExtensionByConnection.Values)
            {
                method(extension);
            }
        }

        private void AutoSyncStyleUpdatesBeforeQueryStatus(BrowserLinkAction browserLinkAction)
        {
            browserLinkAction.Enabled = IsPixelPushingModeEnabled;
            browserLinkAction.Checked = ContinuousSyncModeByProject.GetOrAdd(_connection.Project.UniqueName, p => false);
        }

        private void AutoSyncStyleUpdates(BrowserLinkAction obj)
        {
            Browsers.Client(_connection).Invoke("setContinuousSync", !ContinuousSyncModeByProject.GetOrAdd(_connection.Project.UniqueName, p => false));
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

            if (!projectPath.EndsWith(Path.DirectorySeparatorChar + "", StringComparison.OrdinalIgnoreCase))
            {
                projectPath += Path.DirectorySeparatorChar;
            }

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
            if (locationUrl.EndsWith(".min.css", StringComparison.OrdinalIgnoreCase))
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

        public override void OnDisconnecting(BrowserLinkConnection connection)
        {
            PixelPushingMode extension;
            ExtensionByConnection.TryRemove(connection, out extension);
            _isDisconnecting = true;

            base.OnDisconnecting(connection);
        }

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
                        var ignoreList = IgnoreList.ToList();

                        foreach (var logEntry in result)
                        {
                            var urlGrouped = logEntry.GroupBy(x => x.Url).ToList();
                            var tasks = new List<Task>();

                            foreach (var set in urlGrouped)
                            {
                                var file = GetStyleSheetFileForUrl(set.Key, _connection.Project);

                                if (file == null || ignoreList.Any(x => x.IsMatch(file)))
                                {
                                    continue;
                                }

                                tasks.Add(UpdateSheetRulesAsync(file, set));
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
            var window = WebEssentialsPackage.DTE.ItemOperations.OpenFile(file);
            window.Activate();
            var buffer = ProjectHelpers.GetCurentTextBuffer();
            var flattenedRules = FlattenRules(doc);
            var allEdits = new List<CssRuleBlockSyncAction>();

            doc.IsProcessingUnusedCssRules = false;
            doc.Reparse(buffer.CurrentSnapshot.GetText());

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
                    WebEssentialsPackage.ExecuteCommand("Edit.FormatDocument");
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