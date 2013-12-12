using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public interface IUsageDataSource
    {
        IEnumerable<IStylingRule> AllRules { get; }
        IEnumerable<IStylingRule> UnusedRules { get; }
        IEnumerable<RuleUsage> RuleUsages { get; }

        IEnumerable<Task> GetWarnings();
        IEnumerable<Task> GetWarnings(Uri uri);
        System.Threading.Tasks.Task ResyncAsync();
        void Resync();
    }
}
