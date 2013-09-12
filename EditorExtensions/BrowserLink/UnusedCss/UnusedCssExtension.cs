﻿using System;
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
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Action<UnusedCssExtension>>> BrowserLocationContinuationActions = new ConcurrentDictionary<string, ConcurrentDictionary<string, Action<UnusedCssExtension>>>();
        private static readonly ConcurrentDictionary<BrowserLinkConnection, UnusedCssExtension> ExtensionByConnection = new ConcurrentDictionary<BrowserLinkConnection, UnusedCssExtension>();
        private static readonly HashSet<string> ValidSheetUrls = new HashSet<string>();
        private readonly BrowserLinkConnection _connection;
        private readonly IList<Guid> _operationsInProgress = new List<Guid>();
        private readonly UploadHelper _uploadHelper;

        public UnusedCssExtension(BrowserLinkConnection connection)
        {
            ExtensionByConnection[connection] = this;
            _uploadHelper = new UploadHelper();
            _connection = connection;
            UnusedCssOptions.SettingsUpdated += InstallIgnorePatterns;
        }

        public static List<string> IgnoreList
        {
            get
            {
                var ignorePatterns = WESettings.GetString(WESettings.Keys.UnusedCss_IgnorePatterns) ?? "";

                return ignorePatterns.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
            }
        }

        public static bool IsAnyConnectionAlive { get { return ExtensionByConnection.Count > 0; } }

        public IEnumerable<BrowserLinkAction> Actions
        {
            get
            {
                yield return new BrowserLinkAction("Snapshot Page", SnapshotPage);
                yield return new BrowserLinkAction("Start Recording", ToggleRecordingMode, SetRecordingButtonDisplayProperties);
            }
        }

        public BrowserLinkConnection Connection { get { return _connection; } }

        public bool IsRecording { get; private set; }

        private static List<string> IgnorePatternList
        {
            get { return IgnoreList.Select(FilePatternToRegex).ToList(); }
        }

        public static IEnumerable<string> GetValidSheetUrls()
        {
            HashSet<string> set;

            lock (ValidSheetUrls)
            {
                set = new HashSet<string>(ValidSheetUrls);
            }

            return set;
        }

        public void BlipRecording()
        {
            Clients.Call(_connection, "blipRecording");
        }

        public void EnsureRecordingMode(bool targetRecordingStatus)
        {
            if (IsRecording ^ targetRecordingStatus)
            {
                ToggleRecordingMode();
            }
        }

        [BrowserLinkCallback]
        public async void FinishedRecording(string operationId, string chunkContents, int chunkNumber, int chunkCount)
        {
            SessionResult result;
            var opId = Guid.Parse(operationId);
            if (_uploadHelper.TryFinishOperation(opId, chunkContents, chunkNumber, chunkCount, out result))
            {
                lock (_operationsInProgress)
                {
                    _operationsInProgress.Remove(opId);
                }

                ImportSheets(result.Sheets);

                using (AmbientRuleContext.GetOrCreate())
                {
                    await result.ResolveAsync(this);
                    UsageRegistry.Merge(this, result);
                    MessageDisplayManager.ShowWarningsFor(_connection.Url, _connection.Project, result);
                }
            }
        }

        [BrowserLinkCallback]
        public async void FinishedSnapshot(string operationId, string chunkContents, int chunkNumber, int chunkCount)
        {
            SessionResult result;
            var opId = Guid.Parse(operationId);
            if (_uploadHelper.TryFinishOperation(opId, chunkContents, chunkNumber, chunkCount, out result))
            {
                lock (_operationsInProgress)
                {
                    _operationsInProgress.Remove(opId);
                }

                ImportSheets(result.Sheets);

                using (AmbientRuleContext.GetOrCreate())
                {
                    await result.ResolveAsync(this);
                    UsageRegistry.Merge(this, result);
                    MessageDisplayManager.ShowWarningsFor(_connection.Url, _connection.Project, result);
                }
            }
        }

        [BrowserLinkCallback]
        public void GetIgnoreList()
        {
            Clients.Call(_connection, "installIgnorePatterns", IgnorePatternList);
            //Apply any deferred actions
            //NOTE: There should be some kind of check here to determine whether or not this is a new session for the browser (as the user may have closed the window during the recording session and opened a new browser)
            var appBag = BrowserLocationContinuationActions.GetOrAdd(_connection.AppName, n => new ConcurrentDictionary<string, Action<UnusedCssExtension>>());
            Action<UnusedCssExtension> act;
            if (appBag.TryRemove(_connection.Project.UniqueName, out act))
            {
                act(this);
            }

            if (Any(x => x.Connection.AppName == _connection.AppName && x.IsRecording))
            {
                EnsureRecordingMode(true);
            }
        }

        public override void OnDisconnecting(BrowserLinkConnection connection)
        {
            lock (_operationsInProgress)
            {
                foreach (var opId in _operationsInProgress)
                {
                    _uploadHelper.CancelOperation(opId);
                }
            }

            if (IsRecording)
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

        [BrowserLinkCallback]
        public void SnapshotPage()
        {
            var opId = Guid.NewGuid();
            
            lock (_operationsInProgress)
            {
                _operationsInProgress.Add(opId);
            }

            Clients.Call(_connection, "snapshotPage", opId);
        }

        [BrowserLinkCallback]
        public void ToggleRecordingMode()
        {
            IsRecording = !IsRecording;

            if (!IsRecording)
            {
                Clients.Call(_connection, "stopRecording");
            }
            else
            {
                var opId = Guid.NewGuid();

                lock (_operationsInProgress)
                {
                    _operationsInProgress.Add(opId);
                }

                Clients.Call(_connection, "startRecording", opId);
            }
        }

        internal static void All(Action<UnusedCssExtension> method)
        {
            MessageDisplayManager.DisplaySource = MessageDisplaySource.Project;
            foreach (var extension in ExtensionByConnection.Values)
            {
                method(extension);
            }
        }

        internal static bool Any(Func<UnusedCssExtension, bool> predicate)
        {
            return ExtensionByConnection.Values.Any(predicate);
        }

        private static string FilePatternToRegex(string filePattern)
        {
            return filePattern.Replace(@"\", @"[\\\\/]").Replace(".", @"\.").Replace("*", @"[^\\\\/]*").Replace("?", @"[^\\\\/]?");
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

        private void ImportSheets(IEnumerable<string> sheetUrls)
        {
            var sheets = GetFiles(sheetUrls).Select(x => x.ToLowerInvariant());

            lock (ValidSheetUrls)
            {
                ValidSheetUrls.UnionWith(sheets);
            }
        }

        private void InstallIgnorePatterns(object sender, EventArgs e)
        {
            lock (ValidSheetUrls)
            {
                ValidSheetUrls.Clear();
            }

            UsageRegistry.Reset();
            GetIgnoreList();
            MessageDisplayManager.Refresh();
        }

        private void SetRecordingButtonDisplayProperties(BrowserLinkAction obj)
        {
            obj.ButtonText = IsRecording ? "Stop Recording" : "Start Recording";
        }
    }
}