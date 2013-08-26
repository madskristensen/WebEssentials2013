using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Web.BrowserLink;
using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public static class MessageDisplayManager
    {
        private static IEnumerable<Task> _currentDisplayData;

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

                switch (DisplaySource)
                {
                    case MessageDisplaySource.Project:
                        _currentDisplayData = UsageRegistry.GetWarnings(connection.Project);
                        break;
                    case MessageDisplaySource.Url:
                        _currentDisplayData = UsageRegistry.GetWarnings(connection.Url);
                        break;
                    case MessageDisplaySource.Browser:
                        _currentDisplayData = browserSource.GetWarnings();
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
        }
    }
}
