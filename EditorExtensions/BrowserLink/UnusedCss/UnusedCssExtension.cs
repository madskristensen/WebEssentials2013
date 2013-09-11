using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Web.BrowserLink;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public class UnusedCssExtension : BrowserLinkExtension, IBrowserLinkActionProvider
    {
        private static readonly ConcurrentDictionary<BrowserLinkConnection, UnusedCssExtension> ExtensionByConnection = new ConcurrentDictionary<BrowserLinkConnection, UnusedCssExtension>();
        private readonly HashSet<string> _validSheetUrlsForPage = new HashSet<string>();
        private readonly UploadHelper _uploadHelper;
        private readonly BrowserLinkConnection _connection;
        private bool _isRecording;
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Action<UnusedCssExtension>>> BrowserLocationContinuationActions = new ConcurrentDictionary<string, ConcurrentDictionary<string, Action<UnusedCssExtension>>>();

        internal static bool Any(Func<UnusedCssExtension, bool> predicate)
        {
            return ExtensionByConnection.Values.Any(predicate);
        }

        internal static void All(Action<UnusedCssExtension> method)
        {
            MessageDisplayManager.DisplaySource = MessageDisplaySource.Project;
            foreach (var extension in ExtensionByConnection.Values)
            {
                method(extension);
            }
        }

        public static bool IsAnyConnectionAlive { get { return ExtensionByConnection.Count > 0; } }

        public BrowserLinkConnection Connection { get { return _connection; } }

        public UnusedCssExtension(BrowserLinkConnection connection)
        {
            ExtensionByConnection[connection] = this;
            _uploadHelper = new UploadHelper();
            _connection = connection;
            UnusedCssOptions.SettingsUpdated += InstallIgnorePatterns;
        }

        private void InstallIgnorePatterns(object sender, EventArgs e)
        {
            UsageRegistry.Reset();
            GetIgnoreList();
            MessageDisplayManager.Refresh();
        }

        public override void OnDisconnecting(BrowserLinkConnection connection)
        {
            if (_isRecording)
            {
                var appBag = BrowserLocationContinuationActions.GetOrAdd(_connection.AppName, n => new ConcurrentDictionary<string, Action<UnusedCssExtension>>());

                try
                {
                    appBag.AddOrUpdate(_connection.Project.UniqueName, n => c => c.ToggleRecordingMode(), (n, a) => c => c.ToggleRecordingMode());
                }
                catch (COMException)
                {
                    return;
                }
            }

            UnusedCssExtension extension;
            ExtensionByConnection.TryRemove(connection, out extension);
            UnusedCssOptions.SettingsUpdated -= InstallIgnorePatterns;
        }

        private void SetRecordingButtonDisplayProperties(BrowserLinkAction obj)
        {
            obj.ButtonText = _isRecording ? "Stop Recording" : "Start Recording";
        }

        private void ImportSheets(IEnumerable<string> sheetUrls)
        {
            var sheets = GetFiles(sheetUrls).Select(x => x.ToLowerInvariant());

            lock (_validSheetUrlsForPage)
            {
                _validSheetUrlsForPage.UnionWith(sheets);
            }
        }

        [BrowserLinkCallback]
        public async void FinishedRecording(string operationId, string chunkContents, int chunkNumber, int chunkCount)
        {
            SessionResult result;
            if (_uploadHelper.TryFinishOperation(Guid.Parse(operationId), chunkContents, chunkNumber, chunkCount, out result))
            {
                ImportSheets(result.Sheets);
                await result.ResolveAsync(this);
                UsageRegistry.Merge(this, result);
                MessageDisplayManager.ShowWarningsFor(_connection, result);
            }
        }

        [BrowserLinkCallback]
        public async void FinishedSnapshot(string operationId, string chunkContents, int chunkNumber, int chunkCount)
        {
            SessionResult result;
            if (_uploadHelper.TryFinishOperation(Guid.Parse(operationId), chunkContents, chunkNumber, chunkCount, out result))
            {
                ImportSheets(result.Sheets);
                await result.ResolveAsync(this);
                UsageRegistry.Merge(this, result);
                MessageDisplayManager.ShowWarningsFor(_connection, result);
            }
        }

        public IEnumerable<BrowserLinkAction> Actions
        {
            get
            {
                yield return new BrowserLinkAction("Snapshot Page", SnapshotPage);
                yield return new BrowserLinkAction("Start Recording", ToggleRecordingMode, SetRecordingButtonDisplayProperties);
            }
        }
        
        private IEnumerable<string> GetFiles(IEnumerable<string> locations)
        {
            var project = Connection.Project;
            //TODO: This needs to expand bundles, convert urls to local file names, and move from .min.css files to .css files where applicable
            //NOTE: Project parameter here is for the discovery of linked files, ones that might exist outside of the project structure
            var projectPath = project.Properties.Item("FullPath").Value.ToString();
            var projectUri = new Uri(projectPath, UriKind.Absolute);

            foreach (var location in locations)
            {
                if (location == null)
                {
                    continue;
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
                        continue;
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
                            continue;
                        }

                        locationUri = new Uri(lessFile, UriKind.Relative);
                        Uri.TryCreate(projectUri, locationUri, out realLocation);
                        filePath = realLocation.LocalPath;
                    }
                }
                catch (IOException)
                {
                    continue;
                }

                yield return filePath;
            }
        }

        [BrowserLinkCallback]
        public void ToggleRecordingMode()
        {
            _isRecording = !_isRecording;

            if (!_isRecording)
            {
                Clients.Call(_connection, "stopRecording");
            }
            else
            {
                Clients.Call(_connection, "startRecording", Guid.NewGuid());
            }
        }

        [BrowserLinkCallback]
        public void SnapshotPage()
        {
            Clients.Call(_connection, "snapshotPage", Guid.NewGuid());
        }

        public IEnumerable<string> GetValidSheetUrlsForCurrentLocation()
        {
            return _validSheetUrlsForPage;
        }

        public static IEnumerable<string> GetValidSheetUrls()
        {
            return ExtensionByConnection.Values.SelectMany(x => x._validSheetUrlsForPage).Distinct().ToList();
        }
            
        [BrowserLinkCallback]
        public void ParseSheets(string operationId, string chunkContents, int chunkNumber, int chunkCount)
        {
            List<string> result;
            if (_uploadHelper.TryFinishOperation(Guid.Parse(operationId), chunkContents, chunkNumber, chunkCount, out result))
            {
                var sheets = GetFiles(result).Select(x => x.ToLowerInvariant());

                lock (_validSheetUrlsForPage)
                {
                    _validSheetUrlsForPage.UnionWith(sheets);
                }
            }

            RuleRegistry.GetAllRules(this);

            //Apply any deferred actions
            //NOTE: There should be some kind of check here to determine whether or not this is a new session for the browser (as the user may have closed the window during the recording session and opened a new browser)
            var appBag = BrowserLocationContinuationActions.GetOrAdd(_connection.AppName, n => new ConcurrentDictionary<string, Action<UnusedCssExtension>>());
            Action<UnusedCssExtension> act;
            if (appBag.TryRemove(_connection.Project.UniqueName, out act))
            {
                act(this);
            }
        }

        public static List<string> IgnoreList
        {
            get
            {
                string ignorePatterns = WESettings.GetString(WESettings.Keys.UnusedCss_IgnorePatterns) ?? "";

                return ignorePatterns.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
            }
        }

        private static List<string> IgnorePatternList
        {
            get
            {
                return IgnoreList.Select(FilePatternToRegex).ToList();
            }
        }

        private static string FilePatternToRegex(string filePattern)
        {
            return filePattern.Replace(@"\", @"[\\\\/]").Replace(".", @"\.").Replace("*", @"[^\\\\/]*").Replace("?", @"[^\\\\/]?");
        }

        [BrowserLinkCallback]
        public void GetIgnoreList()
        {
            Clients.Call(_connection, "getLinkedStyleSheetUrls", IgnorePatternList, Guid.NewGuid());
        }

        public void EnsureRecordingMode(bool targetRecordingStatus)
        {
            if (_isRecording ^ targetRecordingStatus)
            {
                ToggleRecordingMode();
            }
        }

        public bool IsRecording { get { return _isRecording; } }
    }
}