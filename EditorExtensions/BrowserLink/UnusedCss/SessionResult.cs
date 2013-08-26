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

        private readonly Lazy<IEnumerable<CssRule>> _allRules;

        private IEnumerable<CssRule> _unusedRules;

        public SessionResult()
        {
            _allRules = new Lazy<IEnumerable<CssRule>>(GetAllRules);
        }

        private void ThrowIfNotResolved()
        {
            if(Volatile.Read(ref _isResolved) == 0)
            {
                throw new InvalidOperationException("Data source must be resolved first");
            }
        }

        public void Resolve(UnusedCssExtension extension)
        {
            if (Interlocked.CompareExchange(ref _isResolved, 1, 0) == 1)
            {
                throw new InvalidOperationException("Data source has already been resolved");
            }

            _extension = extension;
            _ruleUsages = CssRuleRegistry.Resolve(extension, RawUsageData);
            _unusedRules = _allRules.Value.Except(_ruleUsages.Select(x => x.Rule)).ToList();
        }

        public IEnumerable<CssRule> GetAllRules()
        {
            ThrowIfNotResolved();
            return _allRules.Value;
        }

        public IEnumerable<CssRule> GetUnusedRules()
        {
            return _unusedRules;
        }

        public IEnumerable<Task> GetWarnings()
        {
            return _unusedRules.Select(x => x.ProduceErrorListTask(TaskErrorCategory.Warning, _extension.Connection.Project, "Unused CSS rule \"{0}\" in " + _extension.Connection.AppName));
        }

        public IEnumerable<RuleUsage> GetRuleUsages()
        {
            return _ruleUsages;
        }
    
        public IEnumerable<Task> GetWarnings(Uri uri)
        {
            return _unusedRules.Select(x => x.ProduceErrorListTask(TaskErrorCategory.Warning, _extension.Connection.Project, "Unused CSS rule \"{0}\" in " + _extension.Connection.AppName + " on page " + (uri ?? _extension.Connection.Url)));
        }
    }
}