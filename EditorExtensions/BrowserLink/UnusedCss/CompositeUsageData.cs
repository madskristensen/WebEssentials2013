using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Shell;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public class CompositeUsageData : IUsageDataSource
    {
        private readonly UnusedCssExtension _extension;
        private readonly HashSet<RuleUsage> _ruleUsages = new HashSet<RuleUsage>();
        private readonly HashSet<IUsageDataSource> _sources = new HashSet<IUsageDataSource>();
        private readonly object _sync = new object();

        public IEnumerable<IStylingRule> AllRules
        {
            get
            {
                return AmbientRuleContext.GetAllRules();
            }
        }

        public IEnumerable<IStylingRule> GetUnusedRules()
        {
            lock (_sync)
            {
                var unusedRules = new HashSet<IStylingRule>(AllRules);

                unusedRules.ExceptWith(_ruleUsages.Select(x => x.Rule).Distinct());

                return unusedRules.Where(x => !UsageRegistry.IsAProtectedClass(x)).ToList();
            }
        }

        public IEnumerable<RuleUsage> GetRuleUsages()
        {
            lock (_sync)
            {
                return _ruleUsages;
            }
        }

        public CompositeUsageData(UnusedCssExtension extension)
        {
            _extension = extension;
        }

        public void AddUsageSource(IUsageDataSource source)
        {
            lock (_sync)
            {
                _sources.Add(source);
                _ruleUsages.UnionWith(source.GetRuleUsages());
            }
        }

        private IEnumerable<Task> GetWarnings(string formatString)
        {
            var orderedRules = GetUnusedRules().OrderBy(x => x.File).ThenBy(x => x.Line).ThenBy(x => x.Column);

            return orderedRules.Select(x => x.ProduceErrorListTask(TaskErrorCategory.Warning, _extension.Connection.Project, formatString));
        }

        public IEnumerable<Task> GetWarnings()
        {
            return GetWarnings("Unused CSS rule \"{1}\"");
        }

        public IEnumerable<Task> GetWarnings(Uri uri)
        {
            return GetWarnings("Unused CSS rule \"{1}\" on page " + uri);
        }

        public async System.Threading.Tasks.Task ResyncAsync()
        {
            await ResyncSourcesAsync();

            lock (_sync)
            {
                _ruleUsages.Clear();

                foreach (var source in _sources)
                {
                    _ruleUsages.UnionWith(source.GetRuleUsages());
                }
            }
        }

        private async System.Threading.Tasks.Task ResyncSourcesAsync()
        {
            IEnumerable<IUsageDataSource> srcs;

            lock (_sync)
            {
                srcs = _sources.ToList();
            }

            foreach (var source in srcs)
            {
                await source.ResyncAsync();
            }
        }

        private void ResyncSources()
        {
            IEnumerable<IUsageDataSource> srcs;

            lock (_sync)
            {
                srcs = _sources.ToList();
            }

            foreach (var source in srcs)
            {
                source.Resync();
            }
        }

        public void Resync()
        {
            ResyncSources();

            lock (_sync)
            {
                _ruleUsages.Clear();

                foreach (var source in _sources)
                {
                    _ruleUsages.UnionWith(source.GetRuleUsages());
                }
            }
        }
    }
}