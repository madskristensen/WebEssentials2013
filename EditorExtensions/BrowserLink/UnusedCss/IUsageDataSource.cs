using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public interface IUsageDataSource
    {
        IEnumerable<IStylingRule> AllRules { get; }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        IEnumerable<IStylingRule> GetUnusedRules();
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        IEnumerable<RuleUsage> GetRuleUsages();
        IEnumerable<Task> GetWarnings();
        IEnumerable<Task> GetWarnings(Uri uri);
        System.Threading.Tasks.Task ResyncAsync();
        void Resync();
    }
}
