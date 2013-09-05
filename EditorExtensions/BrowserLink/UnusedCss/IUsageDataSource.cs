using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public interface IUsageDataSource
    {
        IEnumerable<IStylingRule> GetAllRules();

        IEnumerable<IStylingRule> GetUnusedRules();

        IEnumerable<Task> GetWarnings();

        IEnumerable<Task> GetWarnings(Uri uri);

        IEnumerable<RuleUsage> GetRuleUsages();

        System.Threading.Tasks.Task ResyncAsync();

        void Resync();
    }
}
