using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public static class MessageDisplayManager
    {
        private static IEnumerable<Task> _currentDisplayData;
        private static IUsageDataSource _lastSource;
        private static Project _lastProject;
        private static Uri _lastUri;

        static MessageDisplayManager()
        {
            WebEssentialsPackage.DTE.Events.SolutionEvents.AfterClosing += SolutionEventsOnAfterClosing;
            WebEssentialsPackage.DTE.Events.SolutionEvents.ProjectRemoved += SolutionEventsOnProjectRemoved;
        }

        private static void SolutionEventsOnProjectRemoved(Project project)
        {
            if (_lastProject == null || project == null)
            {
                return;
            }

            if (project.UniqueName == _lastProject.UniqueName)
            {
                UsageRegistry.Reset();
                Refresh();
            }
        }

        private static void SolutionEventsOnAfterClosing()
        {
            UsageRegistry.Reset();
            Refresh();
        }

        public static MessageDisplaySource DisplaySource { get; set; }
        public static void ShowWarningsFor(Uri uri, Project project, IUsageDataSource browserSource)
        {
            using (ErrorList.UpdateSuspensionContext)
            {
                if (_currentDisplayData != null)
                {
                    foreach (var item in _currentDisplayData)
                    {
                        ErrorList.RemoveItem(item);
                    }
                }

                _lastSource = browserSource ?? _lastSource;
                _lastProject = project ?? _lastProject;
                _lastUri = uri ?? _lastUri;

                if (_lastSource == null || _lastProject == null)
                {
                    _currentDisplayData = null;

                    return;
                }

                _dontRefresh = true;

                try
                {
                    switch (DisplaySource)
                    {
                        case MessageDisplaySource.Project:
                            _currentDisplayData = UsageRegistry.GetWarnings(_lastProject).ToList();

                            break;
                        case MessageDisplaySource.Url:
                            _currentDisplayData = UsageRegistry.GetWarnings(_lastUri).ToList();

                            break;
                        case MessageDisplaySource.Browser:
                            _currentDisplayData = _lastSource.GetWarnings().ToList();

                            break;
                        default:
                            _currentDisplayData = null;

                            return;
                    }

                    foreach (var item in _currentDisplayData)
                    {
                        ErrorList.AddItem(item);
                    }
                }
                finally
                {
                    _dontRefresh = false;
                }
            }
        }

        private static bool _dontRefresh;

        public static void Refresh()
        {
            if (!_dontRefresh)
            {
                ShowWarningsFor(null, null, null);
            }
        }
    }
}
