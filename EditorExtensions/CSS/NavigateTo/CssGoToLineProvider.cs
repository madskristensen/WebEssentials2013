using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Microsoft.VisualStudio.PlatformUI;

namespace MadsKristensen.EditorExtensions.Css
{
    internal sealed class CssGoToLineProvider : DisposableObject, INavigateToItemProvider
    {
        private sealed class Worker : IDisposable
        {
            private readonly CssGoToLineProviderFactory _providerFactory;
            private readonly CancellationTokenSource _cancellationTokenSource;
            private readonly CancellationToken _cancellationToken;
            private readonly INavigateToCallback _navigateToCallback;
            private readonly string _searchValue;
            private int _backgroundProgress;

            internal Worker(CssGoToLineProviderFactory providerFactory, INavigateToCallback navigateToCallback, string searchValue)
            {
                _providerFactory = providerFactory;
                _searchValue = searchValue;
                _navigateToCallback = navigateToCallback;
                _cancellationTokenSource = new CancellationTokenSource();
                _cancellationToken = _cancellationTokenSource.Token;

                ThreadPool.QueueUserWorkItem(DoWork, null);
            }

            internal void Cancel()
            {
                _cancellationTokenSource.Cancel();
            }

            private void DoWork(object unused)
            {
                try
                {
                    var fileList = GetFiles();
                    var parallelOptions = new ParallelOptions();
                    parallelOptions.CancellationToken = _cancellationToken;
                    parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount;

                    Parallel.For(0, fileList.Count, parallelOptions, async i =>
                    {
                        string file = fileList[i];
                        IEnumerable<ParseItem> items = await GetItems(file, _searchValue);
                        foreach (ParseItem sel in items)
                        {
                            _cancellationToken.ThrowIfCancellationRequested();
                            _navigateToCallback.AddItem(new NavigateToItem(_searchValue, NavigateToItemKind.Field, "CSS", _searchValue, new CssGoToLineTag(sel, file), MatchKind.Exact, _providerFactory));
                        }

                        var backgroundProgress = Interlocked.Increment(ref _backgroundProgress);
                        _navigateToCallback.ReportProgress(backgroundProgress, fileList.Count);
                    });
                }
                catch
                {
                    // Don't let exceptions from the background thread reach the ThreadPool.  Swallow them
                    // here and complete the navigate operation
                }
                finally
                {
                    _navigateToCallback.Done();
                }
            }

            private async static Task<IEnumerable<ParseItem>> GetItems(string filePath, string searchValue)
            {
                var cssParser = new CssParser();
                StyleSheet ss = cssParser.Parse(await FileHelpers.ReadAllTextRetry(filePath), true);

                return new CssItemAggregator<ParseItem>
                {
                    (ClassSelector c) => c.Text.Contains(searchValue) ? c : null,
                    (IdSelector c) => c.Text.Contains(searchValue) ? c : null
                }.Crawl(ss).Where(s => s != null);
            }

            private static List<string> GetFiles()
            {
                var projectFolderPath = GetProjectFolderPath();
                var fileList = new List<string>();
                if (string.IsNullOrEmpty(projectFolderPath))
                    return fileList;

                fileList.AddRange(Directory.GetFiles(projectFolderPath, "*.less", SearchOption.AllDirectories));
                fileList.AddRange(Directory.GetFiles(projectFolderPath, "*.css", SearchOption.AllDirectories));

                return fileList.Where(filePath => !IsPathIgnoredBySearch(filePath)).ToList();
            }

            private static bool IsPathIgnoredBySearch(string path)
            {
                return
                    path.IndexOf(".min.", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf(".bundle.", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            /// <summary>
            /// This method will get the folder for the current project.  This will limit the search to the 
            /// current project which is unfortunate.  Should probably extend this to include all projects
            /// in the solution at some point
            /// </summary>
            /// 
            /// <remarks>
            /// Note that this method is always called on a background thread of Visual Studio.  Yet it is
            /// issuing queries to several DTE objects.  All DTE objects are really STA COM objects meaning
            /// that this method ends up running a bit of code on the UI thread of Visual Studio via 
            /// COM marshalling.  It would probably be best if we just ran this one time and cached the
            /// result but for now it doesn't cause a significant delay so not adding the caching overhead
            /// </remarks>
            private static string GetProjectFolderPath()
            {
                string folder = ProjectHelpers.GetRootFolder();

                if (string.IsNullOrEmpty(folder))
                {
                    var doc = WebEssentialsPackage.DTE.ActiveDocument;
                    if (doc != null)
                        folder = ProjectHelpers.GetProjectFolder(WebEssentialsPackage.DTE.ActiveDocument.FullName);
                }

                return folder;
            }

            private void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _cancellationTokenSource.Dispose();
                }
            }

            ~Worker()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        private readonly CssGoToLineProviderFactory _owner;
        private Worker _currentWorker;

        public CssGoToLineProvider(CssGoToLineProviderFactory owner)
        {
            _owner = owner;
        }

        public void StartSearch(INavigateToCallback callback, string searchValue)
        {
            Debug.Assert(_currentWorker == null);

            if (searchValue.Length > 0 && (searchValue[0] == '.' || searchValue[0] == '#'))
            {
                _currentWorker = new Worker(_owner, callback, searchValue);
            }
            else
            {
                callback.Done();
            }
        }

        public void StopSearch()
        {
            var currentWorker = Interlocked.Exchange(ref _currentWorker, null);
            if (currentWorker != null)
            {
                currentWorker.Cancel();
            }
        }
    }
}