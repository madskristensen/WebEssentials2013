using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public class SessionResult : IUsageDataSource, IResolutionRequiredDataSource
    {
        [JsonProperty]
        public List<RawRuleUsage> RawUsageData {get;set;}

        private HashSet<RuleUsage> _ruleUsages;

        private int _isResolved;

        private UnusedCssExtension _extension;

        private void ThrowIfNotResolved()
        {
            if(Volatile.Read(ref _isResolved) == 0)
            {
                throw new InvalidOperationException("Data source must be resolved first");
            }
        }

        public async System.Threading.Tasks.Task ResolveAsync(UnusedCssExtension extension)
        {
            if (Interlocked.CompareExchange(ref _isResolved, 1, 0) == 1)
            {
                throw new InvalidOperationException("Data source has already been resolved");
            }

            _extension = extension;
            _ruleUsages = await CssRuleRegistry.ResolveAsync(extension, RawUsageData);
        }

        public IEnumerable<CssRule> GetAllRules()
        {
            ThrowIfNotResolved();
            return CssRuleRegistry.GetAllRules(_extension);
        }

        public IEnumerable<CssRule> GetUnusedRules()
        {
            return GetAllRules().Except(_ruleUsages.Select(x => x.Rule)).ToList();
        }

        public IEnumerable<Task> GetWarnings()
        {
            return GetUnusedRules().Select(x => x.ProduceErrorListTask(TaskErrorCategory.Warning, _extension.Connection.Project, "Unused CSS rule \"{1}\" in " + _extension.Connection.AppName));
        }

        public IEnumerable<RuleUsage> GetRuleUsages()
        {
            return _ruleUsages;
        }
    
        public IEnumerable<Task> GetWarnings(Uri uri)
        {
            return GetUnusedRules().Select(x => x.ProduceErrorListTask(TaskErrorCategory.Warning, _extension.Connection.Project, "Unused CSS rule \"{1}\" in " + _extension.Connection.AppName + " on page " + (uri ?? _extension.Connection.Url)));
        }
    }
}