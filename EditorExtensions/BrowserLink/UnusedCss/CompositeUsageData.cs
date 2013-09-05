using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System.Collections.Concurrent;

namespace MadsKristensen.EditorExtensions.BrowserLink.UnusedCss
{
    public class CompositeUsageData : IUsageDataSource
    {
        private readonly UnusedCssExtension _extension;
        private readonly HashSet<RuleUsage> _ruleUsages = new HashSet<RuleUsage>();
        private readonly ConcurrentBag<IUsageDataSource> _sources = new ConcurrentBag<IUsageDataSource>();
        private readonly object _sync = new object();

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

        public IEnumerable<IStylingRule> GetAllRules()
        {
            lock (_sync)
            {
                return RuleRegistry.GetAllRules(_extension);
            }
        }

        public IEnumerable<RuleUsage> GetRuleUsages()
        {
            lock (_sync)
            {
                return _ruleUsages;
            }
        }

        public IEnumerable<IStylingRule> GetUnusedRules()
        {
            lock (_sync)
            {
                var unusedRules = new HashSet<IStylingRule>(GetAllRules());

                foreach (var src in _sources)
                {
                    unusedRules.IntersectWith(src.GetUnusedRules());
                }

                return unusedRules;
            }
        }

        public IEnumerable<Task> GetWarnings()
        {
            lock(_sync)
            {
                return GetUnusedRules().Select(x => x.ProduceErrorListTask(TaskErrorCategory.Warning, _extension.Connection.Project, "Unused CSS rule \"{1}\""));
            }
        }
        
        public IEnumerable<Task> GetWarnings(Uri uri)
        {
            lock(_sync)
            {
                return GetUnusedRules().Select(x => x.ProduceErrorListTask(TaskErrorCategory.Warning, _extension.Connection.Project, "Unused CSS rule \"{1}\" on page " + uri));
            }
        }

        public async System.Threading.Tasks.Task ResyncAsync()
        {
            foreach (var source in _sources)
            {
                await source.ResyncAsync();
            }

            lock (_sync)
            {
                _ruleUsages.Clear();

                foreach (var source in _sources)
                {
                    _ruleUsages.UnionWith(source.GetRuleUsages());
                }
            }
        }

        public void Resync()
        {
            foreach (var source in _sources)
            {
                source.Resync();
            }

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