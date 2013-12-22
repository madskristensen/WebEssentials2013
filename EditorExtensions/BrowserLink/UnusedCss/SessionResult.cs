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
        private HashSet<RuleUsage> _ruleUsages;
        private int _isResolved;
        private UnusedCssExtension _extension;

        [JsonProperty]
        public HashSet<RawRuleUsage> RawUsageData { get; private set; }
        [JsonProperty]
        public bool Continue { get; set; }
        [JsonProperty]
        public IEnumerable<string> Sheets { get; private set; }

        public IEnumerable<IStylingRule> AllRules
        {
            get
            {
                ThrowIfNotResolved();

                return AmbientRuleContext.GetAllRules();
            }
        }

        public IEnumerable<RuleUsage> GetRuleUsages()
        {
            return _ruleUsages;
        }

        public IEnumerable<IStylingRule> GetUnusedRules()
        {
            return AllRules.Except(_ruleUsages.Select(x => x.Rule)).Where(x => !UsageRegistry.IsAProtectedClass(x)).ToList();
        }

        public SessionResult()
        {
            RawUsageData = new HashSet<RawRuleUsage>();
            Sheets = new List<string>();
            _ruleUsages = new HashSet<RuleUsage>();
        }

        public SessionResult(UnusedCssExtension extension)
            : this()
        {
            _extension = extension;
            _isResolved = 1;
        }

        private void ThrowIfNotResolved()
        {
            if (Volatile.Read(ref _isResolved) == 0)
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
            _ruleUsages = await RuleRegistry.ResolveAsync(RawUsageData);
        }

        private IEnumerable<Task> GetWarnings(string formatString)
        {
            var orderedRules = GetUnusedRules().OrderBy(x => x.File).ThenBy(x => x.Line).ThenBy(x => x.Column);

            return orderedRules.Select(x => x.ProduceErrorListTask(TaskErrorCategory.Warning, _extension.Connection.Project, formatString));
        }

        public IEnumerable<Task> GetWarnings()
        {
            return GetWarnings("Unused CSS rule \"{1}\" in " + _extension.Connection.AppName);
        }

        public IEnumerable<Task> GetWarnings(Uri uri)
        {
            return GetWarnings("Unused CSS rule \"{1}\" in " + _extension.Connection.AppName + " on page " + (uri ?? _extension.Connection.Url));
        }

        public async System.Threading.Tasks.Task ResyncAsync()
        {
            _ruleUsages = await RuleRegistry.ResolveAsync(RawUsageData);
        }

        public void Resync()
        {
            _ruleUsages = RuleRegistry.Resolve(RawUsageData);
        }

        public void Merge(SessionResult source)
        {
            RawUsageData.UnionWith(source.RawUsageData);
            Resync();
        }
    }
}