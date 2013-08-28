using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Web.BrowserLink;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public class UnusedCssExtension : BrowserLinkExtension, IBrowserLinkActionProvider
    {
        private static readonly ConcurrentDictionary<BrowserLinkConnection, UnusedCssExtension> ExtensionByConnection = new ConcurrentDictionary<BrowserLinkConnection, UnusedCssExtension>();
        private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _validSheetUrlsForPage = new ConcurrentDictionary<string, ConcurrentBag<string>>();
        private readonly UploadHelper _uploadHelper;
        private readonly BrowserLinkConnection _connection;
        private readonly string _currentLocation;
        private bool _isRecording;
        private bool _isAggregatingRecordingData;
        private bool _isRunningShapshot;
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Action<UnusedCssExtension>>> BrowserLocationContinuationActions = new ConcurrentDictionary<string, ConcurrentDictionary<string, Action<UnusedCssExtension>>>();

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
            _currentLocation = connection.Url.ToString().ToLowerInvariant();
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
            UnusedCssExtension extension;
            ExtensionByConnection.TryRemove(connection, out extension);
            UnusedCssOptions.SettingsUpdated -= InstallIgnorePatterns;
        }

        private void SetRecordingButtonDisplayProperties(BrowserLinkAction obj)
        {
            obj.Enabled = !_isAggregatingRecordingData;
            obj.ButtonText = _isRecording ? "Stop Recording" : "Start Recording";
        }

        [BrowserLinkCallback]
        public async void FinishedRecording(string expectLocation, string operationId, string chunkContents, int chunkNumber, int chunkCount)
        {
            if (_currentLocation != expectLocation.ToLowerInvariant())
            {
                return;
            }

            if (_isRecording)
            {
                var appBag = BrowserLocationContinuationActions.GetOrAdd(_connection.AppName, n => new ConcurrentDictionary<string, Action<UnusedCssExtension>>());
                appBag.AddOrUpdate(_connection.Project.UniqueName, n => c => c.ToggleRecordingMode(), (n, a) => c => c.ToggleRecordingMode());
            }

            SessionResult result;
            if (_uploadHelper.TryFinishOperation(Guid.Parse(operationId), chunkContents, chunkNumber, chunkCount, out result))
            {
                await result.ResolveAsync(this);
                UsageRegistry.Merge(this, result);
                MessageDisplayManager.ShowWarningsFor(_connection, result);
            }
        }

        [BrowserLinkCallback]
        public async void FinishedSnapshot(string expectLocation, string operationId, string chunkContents, int chunkNumber, int chunkCount)
        {
            if (_currentLocation != expectLocation.ToLowerInvariant())
            {
                return;
            }

            SessionResult result;
            if (_uploadHelper.TryFinishOperation(Guid.Parse(operationId), chunkContents, chunkNumber, chunkCount, out result))
            {
                await result.ResolveAsync(this);
                UsageRegistry.Merge(this, result);
                MessageDisplayManager.ShowWarningsFor(_connection, result);
            }
        }

        private void ResetCollectionStatuses()
        {
            _isAggregatingRecordingData = false;
            _isRecording = false;
            _isRunningShapshot = false;
        }

        public IEnumerable<BrowserLinkAction> Actions
        {
            get
            {
                yield return new BrowserLinkAction("Snapshot Page", SnapshotPage, SetSnapshotButtonDisplayProperties);
                yield return new BrowserLinkAction("Start Recording", ToggleRecordingMode, SetRecordingButtonDisplayProperties);
            }
        }

        private void ToggleRecordingMode()
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

        public void SnapshotPage()
        {
            Clients.Call(_connection, "snapshotPage", Guid.NewGuid());
        }

        public IEnumerable<string> GetValidSheetUrlsForCurrentLocation()
        {
            var location = _connection.Url.ToString().ToLowerInvariant();
            ConcurrentBag<string> result;
            
            if (!_validSheetUrlsForPage.TryGetValue(location, out result))
            {
                return new string[0];
            }

            return result;
        }

        private void SetSnapshotButtonDisplayProperties(BrowserLinkAction obj)
        {
            obj.Enabled = !_isRunningShapshot;
            obj.ButtonText = _isRunningShapshot ? "Snapshot in progress..." : "Snapshot Page";
        }

        [BrowserLinkCallback]
        public void ParseSheets(string expectLocation, string operationId, string chunkContents, int chunkNumber, int chunkCount)
        {
            if (_currentLocation != expectLocation.ToLowerInvariant())
            {
                return;
            }

            List<string> result;
            if (_uploadHelper.TryFinishOperation(Guid.Parse(operationId), chunkContents, chunkNumber, chunkCount, out result))
            {
                _validSheetUrlsForPage.AddOrUpdate(_connection.Url.ToString().ToLowerInvariant(), u => new ConcurrentBag<string>(result), (u, x) => new ConcurrentBag<string>(result));
            }

            CssRuleRegistry.GetAllRules(this);

            //Apply any deferred actions
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
    }
}