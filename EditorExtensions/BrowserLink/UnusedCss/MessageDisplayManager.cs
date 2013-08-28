using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Web.BrowserLink;
using System.Collections.Generic;
using System.Linq;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public static class MessageDisplayManager
    {
        private static IEnumerable<Task> _currentDisplayData;

        private static IUsageDataSource _lastSource;

        private static BrowserLinkConnection _lastConnection;

        public static MessageDisplaySource DisplaySource { get; set; }
        public static void ShowWarningsFor(BrowserLinkConnection connection, IUsageDataSource browserSource)
        {
            using (ErrorList.GetUpdateSuspensionContext())
            {

                if (_currentDisplayData != null)
                {
                    foreach (var item in _currentDisplayData)
                    {
                        ErrorList.RemoveItem(item);
                    }
                }

                _lastSource = browserSource ?? _lastSource;
                _lastConnection = connection ?? _lastConnection;

                if (_lastSource == null || _lastConnection == null)
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
                            _currentDisplayData = UsageRegistry.GetWarnings(_lastConnection.Project).ToList();
                            break;
                        case MessageDisplaySource.Url:
                            _currentDisplayData = UsageRegistry.GetWarnings(_lastConnection.Url).ToList();
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
                ShowWarningsFor(null, null);
            }
        }
    }
}
